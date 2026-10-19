"""Inspect the embedded PE manifest without loading/executing the Windows binary."""
import hashlib
import json
import struct
import sys
from pathlib import Path
import xml.etree.ElementTree as ET

path = Path(sys.argv[1])
data = path.read_bytes()
pe = struct.unpack_from('<I', data, 60)[0]
if data[pe:pe + 4] != b'PE\0\0':
    raise ValueError('Not a PE executable')
coff = pe + 4
machine, count = struct.unpack_from('<HH', data, coff)
optional_size = struct.unpack_from('<H', data, coff + 16)[0]
optional = coff + 20
magic = struct.unpack_from('<H', data, optional)[0]
directory = optional + (112 if magic == 0x20b else 96)
resource_rva, _ = struct.unpack_from('<II', data, directory + 16)
sections = []
for index in range(count):
    offset = optional + optional_size + index * 40
    virtual_size, address, raw_size, raw_offset = struct.unpack_from('<IIII', data, offset + 8)
    sections.append((address, max(virtual_size, raw_size), raw_offset))

def file_offset(rva):
    for address, size, raw in sections:
        if address <= rva < address + size:
            return raw + rva - address
    raise ValueError('Unmapped resource address')

base = file_offset(resource_rva)
def entries(relative):
    offset = base + relative
    named, numbered = struct.unpack_from('<HH', data, offset + 12)
    return [struct.unpack_from('<II', data, offset + 16 + index * 8) for index in range(named + numbered)]

manifest = next(value for kind, value in entries(0) if kind == 24)
name = entries(manifest & 0x7fffffff)[0][1]
language = entries(name & 0x7fffffff)[0][1]
rva, size = struct.unpack_from('<II', data, base + language)
text = data[file_offset(rva):file_offset(rva) + size].decode('utf-8').strip('\0')
xml = ET.fromstring(text)
level = xml.find('.//{urn:schemas-microsoft-com:asm.v3}requestedExecutionLevel')
if level is None:
    raise ValueError('Execution level is missing')
print(json.dumps({
    'binary': path.name, 'sha256': hashlib.sha256(data).hexdigest(),
    'machine': hex(machine), 'requestedExecutionLevel': level.get('level'),
    'uiAccess': level.get('uiAccess'), 'manifest': text,
}, indent=2))
if level.get('level') != 'asInvoker' or level.get('uiAccess') != 'false':
    raise SystemExit('Unexpected privilege request')
