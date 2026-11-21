const fs=require('fs'),path=require('path'),cp=require('child_process');const root=path.resolve(__dirname,'..');
const edge='C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe';
for(const [name,height]of [['hero',800],['social',640]]){
 const output=path.join(root,'docs/images/'+name+'.png');
 const result=cp.spawnSync(edge,['--headless','--disable-gpu','--no-first-run','--hide-scrollbars','--user-data-dir='+path.join(root,'.cache/brand-browser'),'--window-size=1280,'+height,'--virtual-time-budget=1200','--screenshot='+output,new URL('file:///'+path.join(root,'docs/design/hero.html').replaceAll('\\','/')).href],{encoding:'utf8',timeout:30000,windowsHide:true});
 if(result.status!==0||!fs.existsSync(output))throw Error('Brand rendering failed: '+result.stderr);console.log('Rendered '+name+'.png');
}
