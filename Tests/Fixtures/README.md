# Test fixtures

`sample.png` is a small generated image. The conversion matrix creates all other media and office fixtures in a disposable temporary directory; it never processes user files.

`input.rar` is decoded from libarchive v3.7.7's `libarchive/test/test_read_format_rar.rar.uu`. `normal.rar` is decoded from `test_read_format_rar_compress_normal.rar.uu`. The unmodified upstream fixtures contain links and are not accepted as safe repack inputs by Orbit.

`regular-files.rar` is derived from `input.rar` by removing its RAR3 file-header record named `testlink` (type 0x74). Remaining headers and stored data are preserved. This permits a positive RAR import check alongside explicit unsafe-link tests.

Upstream source: https://github.com/libarchive/libarchive/tree/v3.7.7/libarchive/test

Licensing: libarchive's upstream BSD notices, retained in `Resources/Licenses/libarchive-COPYING`.
