"""Plot measured recovery trajectories and inputs; this is telemetry, not gameplay imagery."""
import json
import sys
from pathlib import Path

sys.path.insert(0,str(Path(__file__).resolve().parent/'python-packages'))
import matplotlib
matplotlib.use('Agg')
import matplotlib.pyplot as plt

root=Path(sys.argv[1]).resolve()
fig,axes=plt.subplots(2,3,figsize=(15,8),layout='constrained')
fig.suptitle('Physical recovery — measured stock-car runs',fontsize=19,weight='bold')
cases=[('wrong-way',9,'Turn around'),('wall',9,'Reverse around a barrier'),('traffic',8,'Wait, then rejoin')]
for column,(case,duration,title) in enumerate(cases):
    folder=root/('stock-'+case)
    track=json.loads((folder/'race.track.json').read_text(encoding='utf-8-sig'))
    evidence=json.loads((folder/'race.recovery.json').read_text(encoding='utf-8-sig'))
    actor=[s for s in track['samples'] if s['id']==0 and s['time']<=duration]
    ax=axes[0,column];ax.axvspan(-8,8,color='#e7ecee');ax.axvline(-8,color='#97a7ad',lw=.8);ax.axvline(8,color='#97a7ad',lw=.8)
    ax.plot([s['roadOffset'] for s in actor],[s['progress'] for s in actor],color='#167e83',lw=2,label='Recovering car')
    ax.scatter(actor[0]['roadOffset'],actor[0]['progress'],marker='s',s=45,color='#172d3c',label='Start',zorder=5)
    ax.scatter(actor[-1]['roadOffset'],actor[-1]['progress'],marker='^',s=65,color='#167e83',label=f'At {duration}s',zorder=5)
    if case=='wall':ax.plot([-2.5,2.5],[-20.5,-20.5],color='#e2763c',lw=6,label='Barrier')
    if case=='traffic':
        other=[s for s in track['samples'] if s['id']==1 and s['time']<=duration]
        ax.plot([s['roadOffset'] for s in other],[s['progress'] for s in other],color='#b55184',lw=1.6,label='Passing car')
        ax.set_ylim(-38,20)
    ax.set_xlim(-12,12);ax.set_xlabel('Lateral offset (m)');ax.set_ylabel('Route progress (m)');ax.set_title(title,fontsize=13,weight='bold');ax.legend(fontsize=8,loc='best')
    ax=axes[1,column];ax.plot([s['time'] for s in actor],[s['speed']*3.6 for s in actor],color='#167e83',lw=2,label='Actual speed')
    ax.plot([s['time'] for s in actor],[s['target']*3.6 for s in actor],color='#6b7a86',ls='--',lw=1,label='Requested speed')
    for a,b in zip(actor,actor[1:]):
        if a['recoveryState']=='Wait for clearance':ax.axvspan(a['time'],b['time'],color='#e7bb55',alpha=.3,lw=0)
    ax.axhline(0,color='#97a7ad',lw=.8);ax.set_xlim(0,duration);ax.set_xlabel('Seconds since release');ax.set_ylabel('Speed (km/h; negative = reversing)');ax.legend(fontsize=8)
    ax.set_title(f"{evidence['assistedResets']} resets · {evidence['contacts']} contacts · {evidence['waitingSeconds']:.1f}s waiting",fontsize=11)
for ax in axes.flat:
    ax.spines[['top','right']].set_visible(False);ax.grid(alpha=.15);ax.set_axisbelow(True)
fig.supxlabel('10 Hz telemetry • route coordinates, not an exact overhead map • yellow = waiting • initial synthetic placement only',fontsize=10)
output=root/'recovery-review.png';fig.savefig(output,dpi=150);print(output)
