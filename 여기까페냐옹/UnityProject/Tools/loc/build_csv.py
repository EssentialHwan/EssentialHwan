import json, csv, re, io, importlib.util
def load(n):
    spec=importlib.util.spec_from_file_location(n, n+'.py'); m=importlib.util.module_from_spec(spec); spec.loader.exec_module(m); return m
TR={}; m4=load('tr4')
for n in ('tr1','tr2','tr3','tr4'): TR.update(load(n).TR)
rows=json.load(open('strings.json'))
keys=[r[0] for r in rows]
out=[]; missing=[]; tokbad=[]
for k,ko,en in rows:
    ko=m4.KO_FIX.get(k,ko)
    if k not in TR: missing.append(k); out.append([k,ko,en,'','']); continue
    e,zh,ja=TR[k]
    if e is not None: en=e
    toks=lambda s:sorted(set(re.findall(r'\{\w+\}',s)))
    for lang,v in (('en',en),('zh',zh),('ja',ja)):
        if toks(v)!=toks(ko): tokbad.append((k,lang,toks(ko),toks(v)))
    out.append([k,ko,en,zh,ja])
for k,(e,zh,ja) in TR.items():
    if k not in keys: out.append([k,m4.KO_NEW[k],e,zh,ja])
extra=[k for k in TR if k not in keys and k not in m4.KO_NEW]
print('missing',missing); print('tokbad',tokbad); print('extra no ko',extra)
print('quotes', [r[0] for r in out if any('"' in c for c in r)])
print('jip-sa', [r[0] for r in out if '집사' in r[1]])
buf=io.StringIO(); w=csv.writer(buf, lineterminator='\n'); w.writerow(['key','ko','en','zh','ja']); w.writerows(out)
open('strings_new.csv','w',encoding='utf-8').write(buf.getvalue()); print(len(out))
