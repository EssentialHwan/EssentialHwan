import json, csv, re, io, importlib.util
def load(n):
    spec=importlib.util.spec_from_file_location(n, n+'.py'); m=importlib.util.module_from_spec(spec); spec.loader.exec_module(m); return m
TR={}; m4=load('tr4')
for n in ('tr1','tr2','tr3','tr4','tr5'): TR.update(load(n).TR)
PATCH=load('tr6').PATCH
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
    P=PATCH.get(k,{}); ko=P.get('ko',ko); en=P.get('en',en); zh=P.get('zh',zh); ja=P.get('ja',ja)
    out.append([k,ko,en,zh,ja])
for k,(e,zh,ja) in TR.items():
    if k not in keys: out.append([k,m4.KO_NEW[k],e,zh,ja])
extra=[k for k in TR if k not in keys and k not in m4.KO_NEW]
print('patch-miss',[k for k in PATCH if k not in keys and k not in TR]); print('missing',missing); print('tokbad',tokbad); print('extra no ko',extra)
print('quotes', [r[0] for r in out if any('"' in c for c in r)])
print('jip-sa', [r[0] for r in out if '집사' in r[1]])
NEW=load('tr7').NEW
NEW.update(load('tr8').NEW)
NEW.update(load('tr9').NEW)
NEW.update(load('tr10').NEW)
NEW.update(load('tr11').NEW)
P10=load('tr10').PATCH
for r in out:
    if r[0] in P10:
        P=P10[r[0]]; r[1]=P.get('ko',r[1]); r[2]=P.get('en',r[2]); r[3]=P.get('zh',r[3]); r[4]=P.get('ja',r[4])
have={r[0] for r in out}
for k,v in NEW.items():
    if k in have: print('DUP',k); continue
    out.append([k,*v])
print('quotes2', [k for k,v in NEW.items() if any('"' in c for c in v)])
buf=io.StringIO(); w=csv.writer(buf, lineterminator='\n'); w.writerow(['key','ko','en','zh','ja']); w.writerows(out)
open('strings_new.csv','w',encoding='utf-8').write(buf.getvalue()); print(len(out))
