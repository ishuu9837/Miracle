"""Generate the native Xcode app and hosted unit-test project without dependencies."""
from pathlib import Path
import hashlib

root = Path(__file__).resolve().parent.parent
project = root / 'Miracle.xcodeproj'
project.mkdir(exist_ok=True)
objects = {}
def ident(name): return hashlib.sha256(name.encode()).hexdigest()[:24].upper()
def q(value): return '"' + str(value).replace('\\', '\\\\').replace('"', '\\"') + '"'
def obj(object_name, isa, **fields):
    key = ident(object_name)
    objects[key] = '{ isa = ' + isa + '; ' + ' '.join(k + ' = ' + v + ';' for k,v in fields.items()) + ' }'
    return key
def array(items): return '(' + ', '.join(items) + (',' if items else '') + ')'
def settings(values): return '{ ' + ' '.join(k+' = '+q(v)+';' for k,v in values.items()) + ' }'

def group(path):
    children=[]
    for item in sorted((root/path).iterdir(), key=lambda x:(x.is_file(),x.name)):
        rel = item.relative_to(root)
        if rel == Path("Docs/SOURCE_REQUIREMENTS.txt"): continue
        if item.is_dir() and path == Path('Resources'):
            children.append(obj('file:'+str(rel), 'PBXFileReference', lastKnownFileType='folder', path=q(item.name), sourceTree=q('<group>')))
        elif item.is_dir(): children.append(group(rel))
        elif item.suffix in ('.swift','.plist','.md','.txt','.png','.icns'):
            typ={'.swift':'sourcecode.swift','.plist':'text.plist.xml','.png':'image.png','.icns':'image.icns'}.get(item.suffix,'text')
            children.append(obj('file:'+str(rel),'PBXFileReference',lastKnownFileType=typ,path=q(item.name),sourceTree=q('<group>')))
    return obj('group:'+str(path),'PBXGroup',children=array(children),path=q(path.name),sourceTree=q('<group>'))
folders=['App','Core','Engines','Services','Features','Persistence','Shared','Resources','Tests','Docs']
groups=[group(Path(x)) for x in folders]
app_product=obj('app-product','PBXFileReference',explicitFileType='wrapper.application',includeInIndex='0',path='Miracle.app',sourceTree='BUILT_PRODUCTS_DIR')
test_product=obj('test-product','PBXFileReference',explicitFileType='wrapper.cfbundle',includeInIndex='0',path='MiracleTests.xctest',sourceTree='BUILT_PRODUCTS_DIR')
products=obj('products','PBXGroup',children=array([app_product,test_product]),name='Products',sourceTree=q('<group>'))
main=obj('main-group','PBXGroup',children=array(groups+[products]),sourceTree=q('<group>'))
def sources(name, paths):
    entries=[]
    for path in paths:
        entries.append(obj('build:'+str(path),'PBXBuildFile',fileRef=ident('file:'+str(path))))
    return obj(name,'PBXSourcesBuildPhase',buildActionMask='2147483647',files=array(entries),runOnlyForDeploymentPostprocessing='0')
app_paths=sorted(p.relative_to(root) for folder in folders[:7] for p in (root/folder).rglob('*.swift'))
test_paths=sorted(p.relative_to(root) for p in (root/'Tests/Unit').rglob('*.swift'))
app_sources=sources('app-sources',app_paths)
test_sources=sources('test-sources',test_paths)
def config_list(name, common):
    configs=[]
    for mode in ['Debug','Release']:
        values=dict(common)
        values.update(SWIFT_OPTIMIZATION_LEVEL='-Onone' if mode=='Debug' else '-O', DEBUG_INFORMATION_FORMAT='dwarf', ONLY_ACTIVE_ARCH='YES')
        if mode=='Debug': values.update(ENABLE_TESTABILITY='YES',SWIFT_ACTIVE_COMPILATION_CONDITIONS='DEBUG')
        configs.append(obj(name+mode,'XCBuildConfiguration',buildSettings=settings(values),name=mode))
    return obj(name+'configs','XCConfigurationList',buildConfigurations=array(configs),defaultConfigurationIsVisible='0',defaultConfigurationName='Release')
resource_builds = [obj('resource:'+name, 'PBXBuildFile', fileRef=ident('file:Resources/'+name)) for name in ['Engines','Licenses','AppIcon.icns','BrandLogo.png']]
resource_phase = obj('app-resources', 'PBXResourcesBuildPhase', buildActionMask='2147483647', files=array(resource_builds), runOnlyForDeploymentPostprocessing='0')
project_configs=config_list('project',dict(ARCHS='arm64',SWIFT_INCLUDE_PATHS='$(SRCROOT)/Core/CArchive',MACOSX_DEPLOYMENT_TARGET='13.0',SDKROOT='macosx',SWIFT_VERSION='6.0',CLANG_ENABLE_MODULES='YES',SWIFT_STRICT_CONCURRENCY='complete'))
app_configs=config_list('app',dict(PRODUCT_NAME='Miracle',PRODUCT_BUNDLE_IDENTIFIER='local.miracle.converter',INFOPLIST_FILE='Resources/Info.plist',CODE_SIGN_STYLE='Automatic',ENABLE_HARDENED_RUNTIME='YES',ENABLE_APP_SANDBOX='NO',OTHER_LDFLAGS='-lsqlite3 -larchive',LD_RUNPATH_SEARCH_PATHS='$(inherited) @executable_path/../Frameworks',COMBINE_HIDPI_IMAGES='YES',SWIFT_OBJC_BRIDGING_HEADER='Resources/Miracle-Bridging-Header.h',CODE_SIGN_ENTITLEMENTS='Resources/Miracle.entitlements'))
test_configs=config_list('tests',dict(PRODUCT_NAME='$(TARGET_NAME)',PRODUCT_BUNDLE_IDENTIFIER='local.miracle.converter.tests',GENERATE_INFOPLIST_FILE='YES',CODE_SIGN_STYLE='Automatic',TEST_HOST='$(BUILT_PRODUCTS_DIR)/Miracle.app/Contents/MacOS/Miracle',BUNDLE_LOADER='$(TEST_HOST)',LD_RUNPATH_SEARCH_PATHS='$(inherited) @executable_path/../Frameworks @loader_path/../Frameworks',OTHER_LDFLAGS='-lsqlite3 -larchive'))
app_target=obj('app-target','PBXNativeTarget',buildConfigurationList=app_configs,buildPhases=array([app_sources,resource_phase]),buildRules='()',dependencies='()',name='Miracle',productName='Miracle',productReference=app_product,productType=q('com.apple.product-type.application'))
proxy=obj('proxy','PBXContainerItemProxy',containerPortal=ident('project'),proxyType='1',remoteGlobalIDString=app_target,remoteInfo='Miracle')
dep=obj('test-dependency','PBXTargetDependency',target=app_target,targetProxy=proxy)
test_target=obj('test-target','PBXNativeTarget',buildConfigurationList=test_configs,buildPhases=array([test_sources]),buildRules='()',dependencies=array([dep]),name='MiracleTests',productName='MiracleTests',productReference=test_product,productType=q('com.apple.product-type.bundle.unit-test'))
project_id=obj('project','PBXProject',attributes='{ LastUpgradeCheck = 1600; BuildIndependentTargetsInParallel = YES; }',buildConfigurationList=project_configs,compatibilityVersion=q('Xcode 14.0'),developmentRegion='en',hasScannedForEncodings='0',knownRegions='(en, Base)',mainGroup=main,productRefGroup=products,projectDirPath=q(''),projectRoot=q(''),targets=array([app_target,test_target]))
(project/'project.pbxproj').write_text('// !$*UTF8*$!\n{ archiveVersion = 1; classes = {}; objectVersion = 56; objects = {\n'+ '\n'.join(key+' = '+value+';' for key,value in objects.items())+'\n}; rootObject = '+project_id+'; }\n')
scheme_dir=project/'xcshareddata/xcschemes';scheme_dir.mkdir(parents=True,exist_ok=True)
def ref(target, name): return f'<BuildableReference BuildableIdentifier="primary" BlueprintIdentifier="{target}" BuildableName="{name}" BlueprintName="{name.split(".")[0]}" ReferencedContainer="container:Miracle.xcodeproj"/>'
app_ref=ref(app_target,'Miracle.app');test_ref=ref(test_target,'MiracleTests.xctest')
(scheme_dir/'Miracle.xcscheme').write_text(f'''<?xml version="1.0" encoding="UTF-8"?>
<Scheme LastUpgradeVersion="1600" version="1.3">
<BuildAction parallelizeBuildables="YES" buildImplicitDependencies="YES"><BuildActionEntries><BuildActionEntry buildForTesting="YES" buildForRunning="YES" buildForProfiling="YES" buildForArchiving="YES" buildForAnalyzing="YES">{app_ref}</BuildActionEntry><BuildActionEntry buildForTesting="YES" buildForRunning="NO" buildForProfiling="NO" buildForArchiving="NO" buildForAnalyzing="NO">{test_ref}</BuildActionEntry></BuildActionEntries></BuildAction>
<TestAction buildConfiguration="Debug" selectedDebuggerIdentifier="Xcode.DebuggerFoundation.Debugger.LLDB" selectedLauncherIdentifier="Xcode.IDEFoundation.Launcher.LLDB" shouldUseLaunchSchemeArgsEnv="YES"><Testables><TestableReference skipped="NO">{test_ref}</TestableReference></Testables></TestAction>
<LaunchAction buildConfiguration="Debug" selectedDebuggerIdentifier="Xcode.DebuggerFoundation.Debugger.LLDB" selectedLauncherIdentifier="Xcode.IDEFoundation.Launcher.LLDB" launchStyle="0" useCustomWorkingDirectory="NO" ignoresPersistentStateOnLaunch="NO" debugDocumentVersioning="YES" debugServiceExtension="internal" allowLocationSimulation="YES"><BuildableProductRunnable runnableDebuggingMode="0">{app_ref}</BuildableProductRunnable></LaunchAction>
<ProfileAction buildConfiguration="Release" shouldUseLaunchSchemeArgsEnv="YES" savedToolIdentifier="" useCustomWorkingDirectory="NO" debugDocumentVersioning="YES"><BuildableProductRunnable runnableDebuggingMode="0">{app_ref}</BuildableProductRunnable></ProfileAction>
<AnalyzeAction buildConfiguration="Debug"/><ArchiveAction buildConfiguration="Release" revealArchiveInOrganizer="YES"/>
</Scheme>''')
print(project)
