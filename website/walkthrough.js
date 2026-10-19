// Illustrative product walkthroughs; no user files or operating-system actions.
(() => {
const root=document.getElementById('walkthrough');
const steps={mac:[
 ['Install Miracle.','Open miracle.dmg and drag Miracle into the Applications folder.','install'],
 ['Open it once.','Open Miracle from Applications. If blocked, review the Mac setup guide below. Miracle then lives in your menu bar.','launch'],
 ['Pick your file.','Select an image in Finder. Your original stays where it is.','file'],
 ['Give it a Shift.','Tap Shift to open the conversion wheel, then choose JPG.','format'],
 ['Meet the new file.','Miracle saves the converted copy beside your original. Open the result in Finder.','done']
],windows:[
 ['Unpack the magic.','Extract the entire Windows ARM64 ZIP into a local folder. Keep the Engines folder and companion files together.','install'],
 ['Hello, Miracle.','Open Miracle.exe inside the extracted folder. The logo intro leads into the main app.','launch'],
 ['Add your file.','Click Add files, or drag an image into the app. Files stay on your computer.','file'],
 ['Pick. Then convert.','Choose the colorful JPG button and click Convert file. Not sure? The optional wheel helps you explore.','format'],
 ['Your result is ready.','Click Show result to reveal the converted copy. The original is preserved.','done']
]};
let platform='mac',index=0,paused=false,inView=false,timer;
const panel=root.querySelector('[role=tabpanel]'),scene=root.querySelector('.tour-scene'),title=root.querySelector('#tour-title'),copy=root.querySelector('#tour-copy'),stepButtons=[...root.querySelectorAll('[data-tour-step]')],platformTabs=[...root.querySelectorAll('[data-tour-platform]')],pause=root.querySelector('.tour-pause');
const logo='<img src="assets/app-icon.png" alt="" width="60" height="60">';
const file=(name,cls='')=>`<div class="tour-file ${cls}"><span>▧</span><b>${name}</b><small>Image file</small></div>`;
function art(kind){
 if(kind==='install')return platform==='mac'?`<div class="tour-window"><div class="tour-titlebar">miracle.dmg</div><div class="tour-install"><div class="tour-app moving-app">${logo}<b>Miracle</b></div><span class="tour-dashes">→ → →</span><div class="tour-app"><span class="tour-folder">▰</span><b>Applications</b></div></div><div class="tour-mini-caption">Drag to Applications</div></div>`:`<div class="tour-window"><div class="tour-titlebar">Downloads / Miracle-Windows-ARM64.zip</div><div class="tour-install"><div class="tour-app"><span class="tour-folder zip-folder">ZIP</span><b>Miracle.zip</b></div><span class="tour-dashes">→ → →</span><div class="tour-app moving-app">${logo}<b>Extract all</b></div></div><div class="tour-mini-caption">Miracle.exe + Engines + companion files</div></div>`;
 if(kind==='launch')return `<div class="tour-launch">${logo}<span class="tour-logo-type">miracle<span>✦</span></span><small>${platform==='mac'?'Applications → Miracle → menu bar':'Miracle.exe → logo intro → ready'}</small><div class="tour-loading"><i></i></div></div>`;
 if(kind==='file')return `<div class="tour-window"><div class="tour-titlebar">${platform==='mac'?'Finder / Pictures':'Miracle / Add files'}</div><div class="tour-file-grid">${file('weekend.png','chosen-file')}${file('portrait.heic')}${file('poster.png')}</div><div class="tour-mini-caption">${platform==='mac'?'1 file selected':'＋ Add files · or drop them here'}</div><span class="tour-pointer">↖</span></div>`;
 if(kind==='format'&&platform==='mac')return `<div class="tour-mac-convert">${file('weekend.png')}<kbd>⇧ <small>shift</small></kbd><div class="tour-ring"><span class="tour-ring-option tour-jpg">JPG</span><span class="tour-ring-option tour-pdf">PDF</span><span class="tour-ring-option tour-webp">WebP</span><span class="tour-ring-option tour-heic">HEIC</span><span class="tour-ring-option tour-tiff">TIFF</span><span class="tour-ring-option tour-avif">AVIF</span><span class="tour-ring-hub">PNG<br>↓<br><b>JPG</b></span></div><span class="tour-pointer">↖</span></div>`;
 if(kind==='format')return `<div class="tour-window tour-app-window"><div class="tour-titlebar">${logo} Miracle · Pick your new form</div><div class="tour-options"><span class="tour-option selected">JPG ✓</span><span class="tour-option">WebP</span><span class="tour-option">HEIC</span><span class="tour-option">TIFF</span><span class="tour-option">AVIF</span><span class="tour-option">PDF</span></div><div class="tour-convert-button">Convert file ↗</div><small class="tour-optional">Not sure? Use the wheel ↗</small><span class="tour-pointer">↖</span></div>`;
 return `<div class="tour-window"><div class="tour-titlebar">${platform==='mac'?'Finder / Pictures':'Miracle / Conversion complete'}</div><div class="tour-result">${file('weekend.png')}<span class="tour-result-arrow">→</span>${file('weekend.jpg','result-file')}</div><div class="tour-success">✓ New format. Same good stuff.</div><div class="tour-mini-caption">${platform==='windows'?'Show result ↗ · saved beside original':'Original preserved · converted copy beside it'}</div></div>`;
}
function playing(){return !paused&&inView&&!document.hidden&&!document.body.classList.contains('motion-paused');}
function schedule(){clearTimeout(timer);const active=playing();root.classList.toggle('tour-playing',active);const globalPaused=document.body.classList.contains('motion-paused');pause.textContent=globalPaused?'Motion paused':paused?'▶ Play walkthrough':'Ⅱ Pause walkthrough';pause.disabled=globalPaused;pause.setAttribute('aria-pressed',String(!paused&&!globalPaused));if(active)timer=setTimeout(()=>{index=(index+1)%5;render()},4800);}
function render(){const [heading,description,kind]=steps[platform][index];root.dataset.platform=platform;root.dataset.scene=kind;title.textContent=heading;copy.textContent=description;scene.innerHTML=art(kind);scene.setAttribute('aria-label',platform==='mac'?'Illustration of Mac step: '+heading:'Illustration of Windows step: '+heading);stepButtons.forEach((b,i)=>{b.classList.toggle('current',i===index);b.setAttribute('aria-current',i===index?'step':'false');b.querySelector('small').textContent=steps[platform][i][0]});root.querySelector('.tour-counter').textContent=String(index+1).padStart(2,'0')+' / 05';schedule();}
function chooseTab(tab){platform=tab.dataset.tourPlatform;index=0;platformTabs.forEach(t=>{t.setAttribute('aria-selected',String(t===tab));t.tabIndex=t===tab?0:-1});panel.setAttribute('aria-labelledby',tab.id);render();}
platformTabs.forEach((tab,i)=>{tab.addEventListener('click',()=>chooseTab(tab));tab.addEventListener('keydown',e=>{if(['ArrowLeft','ArrowRight','Home','End'].includes(e.key)){e.preventDefault();const t=platformTabs[e.key==='Home'?0:e.key==='End'?1:1-i];chooseTab(t);t.focus()}})});
stepButtons.forEach((b,i)=>b.addEventListener('click',()=>{index=i;render()}));
pause.addEventListener('click',()=>{paused=!paused;schedule()});root.querySelector('.tour-replay').addEventListener('click',()=>{index=0;paused=false;render()});
new IntersectionObserver(entries=>{inView=entries[0].isIntersecting;schedule()},{threshold:.25}).observe(root);
document.addEventListener('visibilitychange',schedule);document.addEventListener('miracle-motion-change',schedule);render();
})();

// Native modal preserves download navigation, supports Escape, and restores focus.
(() => {
const dialog=document.getElementById('mac-setup'),download=document.getElementById('dmg-download');let trigger;
function open(from){trigger=from;dialog.querySelector('#mac-setup-title').innerHTML=from===download?'Your download<br><em>is starting.</em>':'A little setup.<br><em>Then a little magic.</em>';if(!dialog.open)dialog.showModal();}
download.addEventListener('click',event=>{if(event.button!==0||event.ctrlKey||event.metaKey||event.shiftKey||event.altKey)return;open(download)});
document.querySelectorAll('[data-mac-setup]').forEach(button=>button.addEventListener('click',()=>open(button)));
dialog.querySelectorAll('[data-close-setup]').forEach(b=>b.addEventListener('click',()=>dialog.close()));
dialog.addEventListener('click',event=>{if(event.target===dialog){const r=dialog.getBoundingClientRect();if(event.clientX<r.left||event.clientX>r.right||event.clientY<r.top||event.clientY>r.bottom)dialog.close()}});
dialog.addEventListener('close',()=>trigger?.focus({preventScroll:true}));
})();
