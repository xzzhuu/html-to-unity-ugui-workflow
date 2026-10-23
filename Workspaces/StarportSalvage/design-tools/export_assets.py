"""Author isolated vector sources from approved component drawings."""
from pathlib import Path
import json
from render_previews import ROOT

ART=ROOT/'shared-art-source'
ART.mkdir(exist_ok=True)
def path(d,w=3):return f'<path d="{d}" fill="none" stroke="white" stroke-width="{w}" stroke-linejoin="round" stroke-linecap="round"/>'
def rect(x,y,w,h,r=0,stroke=False):
    return f'<rect x="{x}" y="{y}" width="{w}" height="{h}" rx="{r}" '+('fill="none" stroke="white" stroke-width="3"' if stroke else 'fill="white"')+'/>'
def circle(x,y,r,stroke=False):
    return f'<circle cx="{x}" cy="{y}" r="{r}" '+('fill="none" stroke="white" stroke-width="3"' if stroke else 'fill="white"')+'/>'
icons={
 'Medkit':rect(23,13,10,30)+rect(13,23,30,10),
 'Battery':rect(15,16,26,27,3,True)+path('M23 12H33')+path('M29 21L24 29H32L27 37',2),
 'Ration':rect(16,13,24,30,3,True)+path('M21 24H35M21 32H31',2),
 'Beacon':path('M20 43L28 18L36 43')+path('M15 21L12 14L17 8M41 21L44 14L39 8',2),
 'Ore':path('M13 32L21 15L36 13L44 30L33 43L17 42Z')+path('M21 15L26 30H44M26 30L17 42',2),
 'Alloy':''.join(path(f'M13 {y}L35 {y-5}L44 {y+1}L22 {y+6}Z',2) for y in [17,27,37]),
 'Circuit':rect(18,18,20,20,0,True)+''.join(path(f'M{v} 12V18M{v} 38V44',2) for v in [22,28,34])+path('M22 25H34',2),
 'Chip':rect(18,18,20,20,0,True)+''.join(path(f'M{v} 12V18M{v} 38V44',2) for v in [22,28,34])+''.join(rect(x,y,4,4) for x,y in [(22,22),(29,22),(22,29),(29,29)]),
 'NavigateBack':path('M15 28H42M15 28L26 16M15 28L26 40',2.5),
 'Close':path('M18 18L38 38M18 38L38 18',2.5),
 'Search':circle(25,23,11,True)+path('M33 31L44 42',2.5),
 'Confirm':path('M14 29L24 39L43 18',4),
 'Risk':path('M28 12L11 43H45Z',2)+path('M28 22V31',2.5)+circle(28,37,1.5),
 'Lock':path('M19 24V19A9 9 0 0 1 37 19V24',2.5)+rect(15,24,26,20,3,True)+path('M28 31V36',2.5),
 'Weight':circle(28,15,5,True)+path('M16 22H40L45 44H11Z',2.5),
 'Credits':circle(28,28,15,True)+path('M33 20H23L20 28L23 36H33',2)}
icons['Energy']=icons['Battery']
plan=json.loads((ROOT/'design-review/sprite-export-plan-v01.json').read_text(encoding='utf-8'))
for asset in plan['assets']:
    name=asset['name'];w,h=asset['pixelSize']
    if name=='Img_RoundedSurface.png':body=rect(0,0,w,h,24);view=f'0 0 {w} {h}'
    elif name=='Img_Control_Background.png':body=rect(0,0,w,h,20);view=f'0 0 {w} {h}'
    elif name=='Tgl_Sound_Track.png':body=rect(0,0,w,h,32);view=f'0 0 {w} {h}'
    elif name=='Img_Circle.png':body=circle(32,32,32);view='0 0 64 64'
    elif name=='Sld_Value_Bar.png':
        body=rect(0,0,w,h,16);view=f'0 0 {w} {h}'
        asset['border']=[16,15,16,15]
    else:
        key=name.removeprefix('Img_').removesuffix('.png').removeprefix('Item')
        body=icons[key];view='0 0 56 56'
    source=ART/name.replace('.png','.svg')
    source.write_text(f'<svg xmlns="http://www.w3.org/2000/svg" width="{w}" height="{h}" viewBox="{view}">{body}</svg>\n',encoding='utf-8')
plan['status']='approved-export-in-progress'
plan['userApproval']='确定'
plan['technicalAdaptations']=['Sld_Value_Bar上下切片从16改为15px，保留中心2px，显示轮廓不变。']
(ROOT/'design-review/sprite-export-plan-v01.json').write_text(json.dumps(plan,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print(f'Authored {len(plan["assets"])} standalone SVG originals; no panel-image slicing.')
