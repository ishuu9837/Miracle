const reduceMotion=matchMedia('(prefers-reduced-motion: reduce)');
const motionToggle=document.querySelector('.motion-toggle');
let motionPaused=reduceMotion.matches;
let motionOverride=false;
function syncMotion(){document.body.classList.toggle('motion-paused',motionPaused);motionToggle.setAttribute('aria-pressed',String(!motionPaused));motionToggle.setAttribute('aria-label',motionPaused?'Enable motion':'Pause motion');motionToggle.innerHTML=(motionPaused?'▶':'Ⅱ')+' <span>Motion '+(motionPaused?'off':'on')+'</span>';if(motionPaused)document.getAnimations().forEach(a=>a.cancel());document.dispatchEvent(new Event('miracle-motion-change'));}
motionToggle.addEventListener('click',()=>{motionOverride=true;motionPaused=!motionPaused;syncMotion()});
reduceMotion.addEventListener('change',()=>{if(!motionOverride){motionPaused=reduceMotion.matches;syncMotion()}});
syncMotion();
const formatButtons=[...document.querySelectorAll('[data-format]')];
const demo=document.getElementById('conversion-demo');
const center=document.querySelector('.wheel-center');
const output=document.getElementById('demo-output');
const status=document.getElementById('demo-file-status');
const filename=document.getElementById('demo-filename');
let chosen='JPG',conversionTimer,conversionRun=0;
function resetPreview(){clearTimeout(conversionTimer);conversionRun++;demo.classList.remove('is-converting','is-complete');center.removeAttribute('aria-busy');filename.textContent='something-good.png';status.textContent='Drag me onto a format.';output.textContent='PNG → '+chosen+' ↗';}
function selectFormat(button){resetPreview();chosen=button.dataset.format;formatButtons.forEach(b=>b.setAttribute('aria-pressed',String(b===button)));formatButtons.forEach(b=>b.classList.toggle('active',b===button));document.getElementById('selected-format').textContent=chosen;center.setAttribute('aria-label','Play sample conversion to '+chosen);output.textContent='PNG → '+chosen+' ↗';if(!motionPaused)document.getElementById('selected-format').animate([{transform:'translateY(8px)',opacity:0},{transform:'translateY(0)',opacity:1}],{duration:250,easing:'ease-out'});}
formatButtons.forEach(button=>{button.setAttribute('aria-pressed',String(button.classList.contains('active')));button.addEventListener('click',()=>selectFormat(button))});
function playConversion(){resetPreview();const run=conversionRun;demo.classList.add('is-converting');center.setAttribute('aria-busy','true');status.textContent='A little file magic…';output.textContent='PREVIEWING PNG → '+chosen+'…';conversionTimer=setTimeout(()=>{if(run!==conversionRun)return;demo.classList.remove('is-converting');demo.classList.add('is-complete');center.removeAttribute('aria-busy');filename.textContent='something-good.'+chosen.toLowerCase();status.textContent='New format. Same good stuff.';output.textContent='✓ '+chosen+' PREVIEW COMPLETE';},motionPaused?150:1150)}
center.addEventListener('click',playConversion);
demo.tabIndex=0;demo.setAttribute('aria-label','Sample conversion demo. Drag the file over a format and release, or choose a format and press Shift to play.');
demo.addEventListener('keydown',event=>{if(event.key==='Shift'&&!event.repeat){event.preventDefault();playConversion()}});

// Pointer capture keeps the gesture continuous while sliding across the wheel.
const fileCard=document.querySelector('.file-chip');
const wheel=document.querySelector('.wheel');
const dragGhost=document.createElement('div');
dragGhost.className='drag-file-ghost';dragGhost.setAttribute('aria-hidden','true');
dragGhost.innerHTML='<span>▧</span><span>PNG <b>→</b> <strong>?</strong></span>';
document.body.append(dragGhost);
let drag=null;
function formatAtPoint(x,y){
  const under=document.elementFromPoint(x,y);
  if(!under||!wheel.contains(under))return null;
  const target=under.closest('[data-format]');
  if(target)return target;
  // Keep the hub a neutral zone so crossing it never commits a random choice.
  const hub=center.getBoundingClientRect();
  if(Math.hypot(x-(hub.left+hub.width/2),y-(hub.top+hub.height/2))<Math.min(hub.width,hub.height)/2)return null;
  return formatButtons.reduce((best,button)=>{
    const rect=button.getBoundingClientRect();
    const distance=Math.hypot(x-rect.left-rect.width/2,y-rect.top-rect.height/2);
    return !best||distance<best.distance?{button,distance}:best;
  },null)?.button??null;
}
function updateDrag(event){
  if(!drag||event.pointerId!==drag.id)return;
  if(!drag.moved&&Math.hypot(event.clientX-drag.x,event.clientY-drag.y)<6)return;
  if(!drag.moved){drag.moved=true;demo.classList.add('is-dragging');dragGhost.classList.add('visible');}
  dragGhost.style.left=event.clientX+'px';dragGhost.style.top=event.clientY+'px';
  const target=formatAtPoint(event.clientX,event.clientY);
  formatButtons.forEach(b=>b.classList.toggle('drag-target',b===target));
  if(target&&drag.target!==target)selectFormat(target);
  drag.target=target;
  dragGhost.classList.toggle('has-target',Boolean(target));
  dragGhost.querySelector('strong').textContent=target?target.dataset.format:'?';
  status.textContent=target?'Release to preview '+target.dataset.format: 'Slide over a format to choose.';
  output.textContent=target?'RELEASE → '+target.dataset.format:'DRAG TO A FORMAT ↓';
}
function endDrag(event,cancel=false){
  if(!drag||event.pointerId!==drag.id)return;
  const current=drag;
  const target=!cancel&&current.moved?formatAtPoint(event.clientX,event.clientY):null;
  drag=null;
  demo.classList.remove('is-dragging');dragGhost.classList.remove('visible','has-target');
  formatButtons.forEach(b=>b.classList.remove('drag-target'));
  if(fileCard.hasPointerCapture(current.id))fileCard.releasePointerCapture(current.id);
  if(target){selectFormat(target);playConversion();}
  else if(current.moved){selectFormat(formatButtons.find(b=>b.dataset.format===current.original));status.textContent='Drag me onto a format.';}
}
fileCard.addEventListener('pointerdown',event=>{
  if(drag||!event.isPrimary||event.button!==0)return;
  resetPreview();fileCard.focus({preventScroll:true});
  drag={id:event.pointerId,x:event.clientX,y:event.clientY,moved:false,target:null,original:chosen};
  fileCard.setPointerCapture(event.pointerId);
});
fileCard.addEventListener('pointermove',updateDrag);
fileCard.addEventListener('pointerup',event=>endDrag(event));
fileCard.addEventListener('pointercancel',event=>endDrag(event,true));
fileCard.addEventListener('lostpointercapture',event=>endDrag(event,true));
fileCard.addEventListener('dragstart',event=>event.preventDefault());
fileCard.addEventListener('keydown',event=>{if(event.key==='Enter'||event.key===' '){event.preventDefault();playConversion();}});
document.addEventListener('keydown',event=>{if(event.key==='Escape'&&drag){event.preventDefault();endDrag({pointerId:drag.id},true)}});
window.addEventListener('blur',()=>{if(drag)endDrag({pointerId:drag.id},true)});

const observer=new IntersectionObserver(entries=>entries.forEach(entry=>{if(entry.isIntersecting){entry.target.classList.add('revealed');observer.unobserve(entry.target)}}),{threshold:.12});
document.querySelectorAll('.magic-heading,.step,.statement h2,.statement-footer,.formats-heading,.format-panel,.open-source>div,.download-content').forEach((el,i)=>{el.classList.add('reveal');el.style.setProperty('--reveal-delay',(el.classList.contains('step')?i%3*80:0)+'ms');observer.observe(el)});
const families={images:{symbol:'▧',caption:'A NEW LOOK FOR EVERY IMAGE.',formats:'JPG <span>↔</span> PNG <span>↔</span> WebP<br>HEIC <span>·</span> TIFF <span>·</span> AVIF',note:'Plus PDF export, with SVG & BMP as inputs. Available options depend on the source file.'},audio:{symbol:'♫',caption:'SAME SOUND. DIFFERENT SHAPE.',formats:'MP3 <span>·</span> M4A <span>·</span> WAV<br>FLAC <span>·</span> OGG <span>·</span> Opus <span>·</span> AIFF',note:'Audio conversion through the bundled media engine. Windows also exports WMA; Mac treats WMA as input only.'},video:{symbol:'▷',caption:'LET THE NEXT FRAME BEGIN.',formats:'MP4 <span>·</span> MOV <span>·</span> MKV<br>WebM <span>·</span> GIF',note:'Plus MP3 audio extraction for videos with an audio track. Windows also exports AVI & WMV; Mac treats them as input only.'},documents:{symbol:'≡',caption:'WORDS THAT GO PLACES.',formats:'DOCX <span>·</span> TXT <span>·</span> Markdown<br>EPUB <span>·</span> PDF <span>·</span> Pages',note:'Office and PDF routes vary by source. Pages export is Mac-only and requires Apple Pages. Windows uses DOCX; PDF text export has no OCR and does not reconstruct layouts.'},more:{symbol:'✳',caption:'A FEW MORE TRICKS IN THE FILE.',formats:'Slides <span>·</span> Spreadsheets<br>Subtitles <span>·</span> Archives',note:'Explore slide exports, spreadsheet formats, SRT/VTT subtitles, and archive packaging. See GitHub for current coverage.'}};
const tabs=[...document.querySelectorAll('[data-family]')];
function selectTab(tab){document.getElementById('format-panel').getAnimations().forEach(a=>a.cancel());if(!motionPaused)document.getElementById('format-panel').animate([{opacity:.3,transform:'translateY(8px)'},{opacity:1,transform:'translateY(0)'}],{duration:300,easing:'ease-out'});tabs.forEach(t=>{t.setAttribute('aria-selected',String(t===tab));t.tabIndex=t===tab?0:-1});const f=families[tab.dataset.family];document.querySelector('.format-symbol').textContent=f.symbol;document.getElementById('family-caption').textContent=f.caption;document.getElementById('family-formats').innerHTML=f.formats;document.getElementById('family-note').textContent=f.note;document.getElementById('format-panel').setAttribute('aria-labelledby',tab.id)}
tabs.forEach((tab,index)=>{tab.id=`tab-${tab.dataset.family}`;tab.setAttribute('aria-controls','format-panel');tab.tabIndex=index===0?0:-1;tab.addEventListener('click',()=>selectTab(tab));tab.addEventListener('keydown',e=>{let next;if(e.key==='ArrowRight')next=(index+1)%tabs.length;if(e.key==='ArrowLeft')next=(index+tabs.length-1)%tabs.length;if(e.key==='Home')next=0;if(e.key==='End')next=tabs.length-1;if(next!==undefined){e.preventDefault();selectTab(tabs[next]);tabs[next].focus()}})});selectTab(tabs[0]);
