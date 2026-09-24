"""Plot recorded solo trajectories; never fabricate a planned/ideal line."""
import json,sys,os
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parent/'python-packages'))
os.environ.setdefault('MPLCONFIGDIR',str(Path(sys.argv[1])/'.matplotlib-cache'))
import matplotlib
matplotlib.use('Agg')
import matplotlib.pyplot as plt
import numpy as np

root=Path(sys.argv[1]);read=lambda p:json.loads(p.read_text(encoding='utf-8-sig'))
analysis=read(root/'analysis.json')
fig,axes=plt.subplots(2,4,figsize=(16,9),layout='constrained')
colors={'off':'#cf7a2c','on':'#087caa'}
for row,loadout in enumerate(['street','club']):
    for column,entry in enumerate(['left','middle','right','deep']):
        ax=axes[row,column];name=f'{loadout}-{entry}';case=analysis['cases'][name]
        fixture=read(root/f'{name}-off/race.traffic.json');start=fixture['start'];end=fixture['corridorEnd']
        for mode in ['off','on']:
            d=read(root/f'{name}-{mode}/race.track.json')
            if mode=='off':
                survey=[s for s in d['survey'] if start-15<=s['distance']<=end+35]
                road=np.array([[s['position']['x'],s['position']['z']] for s in survey])
                guide=np.array([[s['referencePosition']['x'],s['referencePosition']['z']] for s in survey])
                tangent=np.gradient(road,axis=0);tangent/=np.linalg.norm(tangent,axis=1)[:,None]
                normal=np.column_stack((-tangent[:,1],tangent[:,0]))
                left=road+normal*8;right=road-normal*8
                polygon=np.vstack((left,right[::-1]));ax.fill(polygon[:,0],polygon[:,1],color='#edf0f1',zorder=0)
                for edge in [left,right]:ax.plot(edge[:,0],edge[:,1],color='#bfc7cb',lw=.7)
                ax.plot(guide[:,0],guide[:,1],color='#879399',ls=':',lw=1,label='Reference guide')
            samples=[s for s in d['samples'] if s['id']==0 and s['time']>=0 and start-1<=s['progress']<=end+20]
            points=np.array([[s['position']['x'],s['position']['z']] for s in samples])
            ax.plot(points[:,0],points[:,1],color=colors[mode],ls='--' if mode=='off' else '-',lw=1.7,label='Approved controller' if mode=='off' else 'Dynamic rejoin')
            ax.scatter(*points[0],s=18,color=colors[mode],zorder=4)
        delta=case['cornerSecondsDelta'];caption='No measured exit' if delta is None else f'Exit time change: {delta:+.2f}s'
        failed=not all(analysis['checks'][name].values())
        ax.set_title(f'{loadout.title()} · {entry}\n{caption}',fontsize=11,color='#a13232' if failed else '#223840')
        ax.set_aspect('equal',adjustable='datalim');ax.grid(alpha=.12);ax.tick_params(labelsize=7)
        ax.set_xlabel('World x (m)',fontsize=8)
        if column==0:ax.set_ylabel('World z (m)',fontsize=8)
handles,labels=axes[0,0].get_legend_handles_labels()
fig.legend(handles,labels,loc='outside lower center',ncol=3,frameon=False)
fig.suptitle('Dynamic rejoin study — actual recorded corner trajectories\nDots mark the declared starts; curves show car centres, not collision envelopes.',fontsize=14)
fig.savefig(root/'corner-trajectories.png',dpi=150)
print(root/'corner-trajectories.png')
