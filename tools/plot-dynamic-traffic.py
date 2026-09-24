"""Recorded multi-car corner paths; no synthetic motion or collision claims."""
import json,os,sys
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parent/'python-packages'))
root=Path(sys.argv[1]);os.environ.setdefault('MPLCONFIGDIR',str(root/'.matplotlib-cache'))
import matplotlib
matplotlib.use('Agg')
import matplotlib.pyplot as plt
import numpy as np
read=lambda p:json.loads(p.read_text(encoding='utf-8-sig'))
fig,axes=plt.subplots(2,3,figsize=(13,9),layout='constrained')
colours=['#007b9f','#e68a2e','#9f5184']
for col,name in enumerate(['corner-pair-left','corner-pair-right','corner-three']):
    for row,mode in enumerate(['off','on']):
        ax=axes[row,col];folder=root/f'{name}-{mode}';t=read(folder/'race.track.json');f=read(folder/'race.traffic.json')
        start,end=f['start'],f['corridorEnd']
        survey=[s for s in t['survey'] if start-15<=s['distance']<=end+30]
        road=np.array([[s['position']['x'],s['position']['z']] for s in survey]);tangent=np.gradient(road,axis=0);tangent/=np.linalg.norm(tangent,axis=1)[:,None]
        normal=np.column_stack((-tangent[:,1],tangent[:,0]));a=road+normal*8;b=road-normal*8;polygon=np.vstack((a,b[::-1]))
        ax.fill(polygon[:,0],polygon[:,1],color='#edf0f1')
        for edge in [a,b]:ax.plot(edge[:,0],edge[:,1],color='#bfc7cb',lw=.7)
        for i in range(3 if name=='corner-three' else 2):
            samples=[s for s in t['samples'] if s['id']==i and s['time']>=0 and start-1<=s['progress']<=end+15]
            xy=np.array([[s['position']['x'],s['position']['z']] for s in samples]);ax.plot(xy[:,0],xy[:,1],color=colours[i],lw=1.6,label=f'Car {i}'+(' · club' if i==0 else ' · street'))
            ax.scatter(*xy[0],s=20,color=colours[i])
        ax.set_title(name.replace('corner-','').replace('-',' ')+' · '+('Reference' if mode=='off' else 'Dynamic traffic'))
        ax.set_aspect('equal',adjustable='datalim');ax.grid(alpha=.12);ax.set_xlabel('World x (m)');ax.set_ylabel('World z (m)')
handles,labels=axes[0,2].get_legend_handles_labels();fig.legend(handles,labels,loc='outside lower center',ncol=3,frameon=False)
fig.suptitle(f"Prototype {root.name.rsplit('-',1)[-1]} - recorded side-by-side corner trajectories\nCar centres only; unresolved decisions remain failures even when paths look contained.")
fig.savefig(root/'corner-trajectories.png',dpi=150);print(root/'corner-trajectories.png')
