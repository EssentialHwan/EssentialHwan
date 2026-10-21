import csv, os, glob
from fontTools.ttLib import TTFont, newTable
from fontTools.merge import Merger
from fontTools import subset
from fontTools.pens.ttGlyphPen import TTGlyphPen
from fontTools.pens.cu2quPen import Cu2QuPen
rows=list(csv.reader(open('strings_new.csv',encoding='utf-8')))[1:]
need=set(''.join(r[3] for r in rows)) | set(chr(c) for c in range(32,127)) | set('★☆♥△○…▶×·☼「」『』（）！？，。、：；～—–‘’“”▼▲←→①②③%+-/')
need.discard('\n')
D='fpf/package/dist/12px/proportional/zh_hans'
picked=[]
for f in sorted(glob.glob(D+'/*.woff2')):
    t=TTFont(f); cm=t.getBestCmap()
    if any(ord(c) in cm for c in need):
        for tag in ('GPOS','GSUB','vhea','vmtx','GDEF'):
            if tag in t: del t[tag]
        p=f.replace('.woff2','.otf').replace(D,'tmpfont'); os.makedirs('tmpfont',exist_ok=True); t.flavor=None; t.save(p); picked.append(p)
print('subsets',len(picked))
m=Merger().merge(picked)
m.save('fusion_merged.otf')
opts=subset.Options(); opts.layout_features=[]; opts.notdef_outline=True; opts.name_IDs=['*']
s=subset.Subsetter(opts); s.populate(unicodes=[ord(c) for c in need]); s.subset(m)
cm=m.getBestCmap(); print('missing after subset', ''.join(c for c in need if ord(c) not in cm))
# CFF → TrueType(glyf)
gs=m.getGlyphSet(); order=m.getGlyphOrder()
glyf=newTable('glyf'); glyf.glyphs={}; glyf.glyphOrder=order
for g in order:
    pen=TTGlyphPen(gs); gs[g].draw(Cu2QuPen(pen, 1.0, reverse_direction=True)); glyf[g]=pen.glyph()
m['glyf']=glyf; m['loca']=newTable('loca')
del m['CFF ']
m['maxp'].tableVersion=0x00010000
for a in ('maxZones','maxTwilightPoints','maxStorage','maxFunctionDefs','maxInstructionDefs','maxStackElements','maxSizeOfInstructions','maxComponentElements','maxPoints','maxContours','maxCompositePoints','maxCompositeContours','maxComponentDepth'):
    setattr(m['maxp'],a,0)
m['maxp'].maxZones=1
m['head'].glyphDataFormat=0
m['post'].formatType=3.0
m.sfntVersion='\x00\x01\x00\x00'
m['name'].setName('FusionPixelZh',1,3,1,0x409); m['name'].setName('FusionPixelZh',4,3,1,0x409); m['name'].setName('FusionPixelZh',6,3,1,0x409)
m.save('FusionPixelZh.ttf')
t=TTFont('FusionPixelZh.ttf'); print('glyphs',len(t.getGlyphOrder()),'size',os.path.getsize('FusionPixelZh.ttf'))
