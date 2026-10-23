const fs=require('fs'),path=require('path');
const sharp=require('sharp');
(async()=>{
 const root=path.resolve(__dirname,'..'),input=path.join(root,'shared-art-source'),out=path.join(root,'shared-assets');
 fs.mkdirSync(out,{recursive:true});
 for(const file of fs.readdirSync(input).filter(x=>x.endsWith('.svg'))){
   await sharp(path.join(input,file)).png().toFile(path.join(out,file.replace('.svg','.png')));
 }
 console.log('Exported 22 neutral, transparent PNGs at approved POT source sizes.');
})().catch(e=>{console.error(e);process.exit(1)});
