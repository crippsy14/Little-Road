# Evaluate a track design written as turtle pieces: closure, constraints, geometry metrics and a class C speed profile.
# The track-design skill (.claude/skills/track-design) uses it; defaults are Bramblewick Lanes' frame (docs/82).
# Usage (Blender's Python):  python tools/track-eval.py pieces.txt --name mine [--start 300,0 --heading 90 --target -260,0
#                            --street 560 --village -230,270,60 --land -540,660,-5,840] [--out review/tmp/mine.json]
#   --loop: a closed circuit made only of pieces (no street, no junction turns): it must end at --target (normally the
#           start) on the start heading; the wrap-round straight counts as one. --p2p: a point-to-point road (a hill
#           climb): no closure, no L1/L2, no junction turns; --climb <m> reports the average gradient.
# Pieces, one per line or ';'-separated: "S 40" straight, "A 28 70" arc (degrees, + left; radius m), "M Name" marker,
# "S L1" / "S L2" the two straights solved so the track ends exactly at --target (make them ~70-110 degrees apart).
import math,json,sys,re,argparse
ap=argparse.ArgumentParser();ap.add_argument('pieces');ap.add_argument('--name',default='design')
ap.add_argument('--start',default='300,0');ap.add_argument('--heading',type=float,default=90);ap.add_argument('--target',default='-260,0')
ap.add_argument('--street',type=float,default=560,help='fixed start/finish straight length added to the lap (0 if none)')
ap.add_argument('--village',default='-230,270,60',help='a0,a1,nmax: the lane may not pass n<nmax between a0 and a1')
ap.add_argument('--land',default='-540,660,-5,840',help='amin,amax,nmin,nmax');ap.add_argument('--out',default=None)
ap.add_argument('--loop',action='store_true');ap.add_argument('--p2p',action='store_true');ap.add_argument('--climb',type=float,default=0)
args=ap.parse_args();txt=open(args.pieces).read();name=args.name
START=tuple(float(v) for v in args.start.split(','));TARGET=tuple(float(v) for v in args.target.split(','))
VA0,VA1,VN=(float(v) for v in args.village.split(','));LA0,LA1,LN0,LN1=(float(v) for v in args.land.split(','))
items=[t.strip() for t in re.split(r'[;\n]',txt) if t.strip() and not t.strip().startswith('#')]
def pieces(L1,L2):
    out=[]
    for t in items:
        f=t.split();k=f[0].upper()
        if k=='S':v=f[1];out.append(('S',L1 if v=='L1' else L2 if v=='L2' else float(v)))
        elif k=='A':out.append(('A',float(f[1]),float(f[2])))
        elif k=='M':out.append(('M',' '.join(f[1:])))
    return out
def run(ps,start=None,heading=None,step=1.0):
    start=start or START;heading=args.heading if heading is None else heading
    x,y=start;h=math.radians(heading);pts=[(x,y)];curv=[0.0];marks=[]
    for p in ps:
        if p[0]=='S':
            n=max(1,int(round(p[1]/step)))
            for i in range(n):x+=math.cos(h)*p[1]/n;y+=math.sin(h)*p[1]/n;pts.append((x,y));curv.append(0.0)
        elif p[0]=='A':
            ang=math.radians(p[1]);r=p[2];L=abs(ang)*r;n=max(1,int(round(L/step)))
            for i in range(n):h+=ang/n;x+=math.cos(h-ang/n/2)*L/n;y+=math.sin(h-ang/n/2)*L/n;pts.append((x,y));curv.append(math.copysign(1/r,p[1]))
        else:marks.append((p[1],x,y))
    return pts,curv,math.degrees(h)%360,marks
target=TARGET
def end(L1,L2):return run(pieces(L1,L2))[0][-1]
if args.p2p:L1=L2=0;args.street=0
else:
    e0=end(200,200);e1=end(300,200);e2=end(200,300)
    d1=((e1[0]-e0[0])/100,(e1[1]-e0[1])/100);d2=((e2[0]-e0[0])/100,(e2[1]-e0[1])/100)
    rx,ry=target[0]-e0[0],target[1]-e0[1];det=d1[0]*d2[1]-d1[1]*d2[0]
    L1=200+(rx*d2[1]-ry*d2[0])/det;L2=200+(d1[0]*ry-d1[1]*rx)/det
if args.loop:args.street=0
pts,curv,hend,marks=run(pieces(L1,L2))
lane=len(pts)-1  # 1 m steps (approx)
lane=sum(math.dist(pts[i],pts[i+1]) for i in range(len(pts)-1))
# full lap: street W->E (560 m straight) + E corner (left ~90 at the T) + lane + W corner (left onto street)
lap=lane+args.street
issues=[]
if not args.p2p and (L1<20 or L2<20):issues.append('a solved straight is too short (L1 %.0f, L2 %.0f)'%(L1,L2))
if args.loop:
    dh=(hend-args.heading+180)%360-180
    if abs(dh)>8:issues.append('closes at heading %.0f, %.0f off the start heading'%(hend,dh))
elif not args.p2p and not (195<=hend<=305):issues.append('arrives at W heading %.0f (want 200-300)'%hend)
xs=[p[0] for p in pts];ys=[p[1] for p in pts]
if min(xs)<LA0 or max(xs)>LA1 or max(ys)>LN1 or min(ys)<LN0:issues.append('outside the land: a %.0f..%.0f n %.0f..%.0f'%(min(xs),max(xs),min(ys),max(ys)))
inv=[] if args.loop or args.p2p else [p for i,p in enumerate(pts) if VA0<p[0]<VA1 and p[1]<VN and 40<i<len(pts)-40]
if inv:issues.append('%d m through the village (n<60, a in -230..270)'%len(inv))
mind=1e9
for i in range(0,len(pts),3):
    for j in range(i+90,len(pts),3):
        if args.loop and len(pts)-j+i<90:continue   # (a loop's end meets its start)
        d=math.dist(pts[i],pts[j])
        if d<mind:mind=d;where=pts[i]
if mind<20:issues.append('lane comes within %.0f m of itself near (%.0f,%.0f)'%(mind,where[0],where[1]))
# the two junction turns (street->lane at E: 0->90 left; lane->street at W: hend->360) count as corners of radius ~20
wturn=(360-hend)%360
# geometry metrics over the whole lap
corners=[];cur=None
for p in pieces(L1,L2):
    if p[0]=='A':
        sgn=1 if p[1]>0 else -1
        if cur and cur['sgn']==sgn:cur['deg']+=abs(p[1]);cur['radii'].append(p[2]);cur['len']+=math.radians(abs(p[1]))*p[2]
        else:
            if cur:corners.append(cur)
            cur={'sgn':sgn,'deg':abs(p[1]),'radii':[p[2]],'len':math.radians(abs(p[1]))*p[2]}
    elif p[0]=='S' and p[1]>=25:
        if cur:corners.append(cur);cur=None
if cur:corners.append(cur)
if args.loop and len(corners)>1 and pieces(L1,L2)[0][0]=='A' and pieces(L1,L2)[-1][0]=='A' and corners[0]['sgn']==corners[-1]['sgn']:
    c=corners.pop();corners[0]['deg']+=c['deg'];corners[0]['radii']+=c['radii'];corners[0]['len']+=c['len']
if not (args.loop or args.p2p):corners=[{'sgn':1,'deg':90,'radii':[20],'len':31}]+corners+[{'sgn':1,'deg':wturn,'radii':[20],'len':math.radians(wturn)*20}]
total=sum(c['deg'] for c in corners);curved=sum(c['len'] for c in corners)
hist={'<30':0,'30-60':0,'60-100':0,'100-150':0,'>150':0}
for c in corners:
    d=c['deg'];hist['<30' if d<30 else '30-60' if d<60 else '60-100' if d<100 else '100-150' if d<150 else '>150']+=1
rad={'<30':0,'30-60':0,'60-120':0,'120-250':0,'>250':0}
for c in corners:
    r=min(c['radii']);rad['<30' if r<30 else '30-60' if r<60 else '60-120' if r<120 else '120-250' if r<250 else '>250']+=1
changes=sum(1 for i in range(1,len(corners)) if corners[i]['sgn']!=corners[i-1]['sgn'])
lefts=sum(1 for c in corners if c['sgn']>0);rights=len(corners)-lefts
straights=[p[1] for p in pieces(L1,L2) if p[0]=='S']
# merge consecutive straights
merged=[];acc=0
for p in pieces(L1,L2):
    if p[0]=='S':acc+=p[1]
    elif p[0]=='A':
        if acc:merged.append(acc);acc=0
if acc:merged.append(acc)
if args.loop and len(merged)>1 and pieces(L1,L2)[0][0]=='S' and pieces(L1,L2)[-1][0]=='S':merged[0]+=merged.pop()   # (the straight through the line)
longest=max(merged+[args.street]);second=sorted(merged+[args.street])[-2] if len(merged)>1 else 0
# speed profile: point mass, grip 0.95 g, accel ~2.6 m/s^2 falling with speed, brakes 8 m/s^2 (grip-limited), vmax 42
g=9.81;mu=.95;vmax=42.0
kap=([abs(k) for k in curv][-200:] if args.loop else [0.0]*200)+[abs(k) for k in curv]  # (run-in: the lap's own end for a loop; 200 m of street otherwise)
n=len(kap);vlim=[min(vmax,math.sqrt(mu*g/k)) if k>0 else vmax for k in kap]
v=vlim[:]
for i in range(1,n):v[i]=min(v[i],math.sqrt(v[i-1]**2+2*max(.3,2.8*(1-v[i-1]/vmax))*1.0))
for i in range(n-2,-1,-1):v[i]=min(v[i],math.sqrt(v[i+1]**2+2*7.5*1.0))
lanev=v[200:];t=sum(1.0/max(1,x) for x in lanev)+args.street/30.0
slow=min(lanev);fast=max(lanev)
res={'name':name,'L1':round(L1),'L2':round(L2),'arrive_heading':round(hend),'lane_m':round(lane),'lap_m':round(lap),'issues':issues,
     'corners':len(corners),'corners_per_km':round(len(corners)/(lap/1000),1),'total_turning_deg':round(total),'turning_deg_per_km':round(total/(lap/1000)),
     'curved_share':round(curved/lap,2),'angle_hist':hist,'radius_hist':rad,'direction_changes':changes,'left_right':(lefts,rights),
     'longest_straight_m':round(longest),'longest_share':round(longest/lap,2),'slowest_mps':round(slow,1),'fastest_mps':round(fast,1),'est_lap_s':round(t,1),
     'self_clearance_m':round(mind),'second_straight_share_of_longest':round(second/longest,2) if longest else 0}
if args.climb:res['avg_gradient_pct']=round(100*args.climb/lap,1)
print(json.dumps(res,indent=1))
json.dump({'pts':pts,'marks':marks,'res':res},open(args.out or name+'.json','w'))
