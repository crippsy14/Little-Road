# The parts ladder for every class (docs/47): writes unity/Assets/Resources/Data/parts.json (schema 2).
# Usage: python tools/parts-ladder.py
# Existing part IDs are kept (saves refer to them). Each class has Good / Better / Best per slot plus
# local specialities and sidegrades. Engines carry real specs (layout, revs, torque curve points) and a
# sound fingerprint; turbos their spool; gearboxes their gears, spread and shift. Physics numbers stay in
# the game's scale: engine power_limit (top-speed capability) and accel (launch), turbo power/torque
# multipliers, tyre road/dirt grip, brakes, steering response/yaw, frame mass.
import json, os

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, 'unity', 'Assets', 'Resources', 'Data', 'parts.json')
CLASSES = ['C', 'B', 'A', 'S', 'E']
CLASS_PRICE = {'C': 1, 'B': 2.6, 'A': 5.4, 'S': 10, 'E': 17}
STEP_PRICE = [.35, .65, 1.0]
parts = []

def add(pid, slot, name, hint, price, tier='C', shop=None, **stats):
    p = {'id': pid, 'slot': slot, 'name': name, 'hint': hint, 'price': int(round(price / 10.0) * 10), 'tier': tier}
    if shop: p['shop'] = shop
    p.update(stats)
    parts.append(p)

def ladder_price(base, cls, step):
    return base * CLASS_PRICE[cls] * STEP_PRICE[step]

# ---------------------------------------------------------------- engines
# (id, name, hint, price, class, shop, power_limit, accel, layout, cylinders, litres, idle, peakTorqueRpm,
#  peakPowerRpm, redline, hp, nm, firing, exhaust, intake, hybrid)
# Recorded engine for each part (grain banks in unity/Assets/Resources/Engines, built by tools/engine-grains.py
# from tools/engine-banks.json; docs/47). Matching cylinder counts where the recordings allow; a bank shared
# between classes gets a different exhaust treatment from the part's exhaust type.
ENGINE_SOUND = {
    # Chosen by the in-game engine scorecard (docs/55): each engine's own test drive in the game, scored against how the
    # real recording behaves, among recordings that reach the engine's revs. Within a class every engine has its own
    # recording; V8s keep V8 recordings where one scores (the Twelve-hour, the Duke's car, is the Ferrari 458's V8).
    # No high-revving six was found: the V6s take the Citroen C5 (a family car's engine, to 6,200 rpm; whether the
    # recording is its four or its V6 is unclear), the McLaren 720S (twin-turbo V8) and the rally cars.
    'engine_stock': 'citroen-c2', 'engine_workshop': 'golf', 'engine_club': 'escort', 'engine_sport16': 'piazza', 'engine_valley': 'citroen-c5',
    'engine_b_twincam': 'piazza', 'engine_b_highcomp': 'ds3-wrc', 'engine_b_v6': 'mclaren-720s',
    'engine_a_touring': 'escort', 'engine_a_evo': 'ds3-wrc', 'engine_a_works': 'xsara-wrc',
    'engine_s_v6': 'ds3-wrc', 'engine_s_v8': 'ferrari-458', 'engine_s_v10': 'viper',
    'engine_e_v6': 'puma-rally1', 'engine_e_v8': 'mclaren-720s', 'engine_e_12h': 'ferrari-458', 'engine_e_economy': 'citroen-c5',
}



ENGINES = [
    ('engine_stock', 'Stock 1.2', 'A modest, dependable starting motor.', 0, 'C', None, 24, 3.0, 'I4', 4, 1.2, 850, 3500, 5500, 6000, 68, 105, 'even', 'road', 'box', False),
    ('engine_workshop', 'Workshop tune', 'Stronger starts and hill pulling.', 450, 'C', None, 26.5, 3.4, 'I4', 4, 1.2, 900, 3800, 5900, 6400, 82, 115, 'even', 'road', 'box', False),
    ('engine_club', 'Club motor 1.4', 'Revs harder and pulls longer.', 1000, 'C', None, 29, 3.8, 'I4', 4, 1.4, 900, 4200, 6300, 6800, 105, 135, 'even', 'sport', 'cone', False),
    ('engine_sport16', 'Sport 1.6', 'The best a Street car can buy: sixteen valves and a free-flowing exhaust.', 1500, 'C', None, 31, 4.2, 'I4', 4, 1.6, 950, 4800, 6800, 7200, 128, 155, 'even', 'sport', 'cone', False),
    ('engine_valley', 'Valley V6 2.5', 'Built in Hillcrest: big pull out of every hairpin and up every hill.', 1800, 'C', 'hillcrest', 32, 4.6, 'V6', 6, 2.5, 800, 3600, 5800, 6600, 168, 230, 'even', 'sport', 'box', False),
    ('engine_b_twincam', 'Twin-cam 16V 1.8', 'Double overhead cams: loves the top of the rev range.', None, 'B', None, 42, 5.0, 'I4', 4, 1.8, 950, 5200, 7200, 7800, 160, 175, 'even', 'sport', 'cone', False),
    ('engine_b_highcomp', 'High-comp twin-cam 2.0', 'High compression, lumpy cams, a hard edge at 8,000.', None, 'B', None, 45, 5.4, 'I4', 4, 2.0, 1000, 5600, 7700, 8200, 195, 205, 'even', 'race', 'cone', False),
    ('engine_b_v6', 'Tuner V6 3.0', 'Six smooth cylinders and torque everywhere.', None, 'B', None, 48, 5.8, 'V6', 6, 3.0, 900, 4800, 7000, 7600, 250, 300, 'even', 'sport', 'cone', False),
    ('engine_a_touring', 'Touring 2.0', 'A works touring motor: loud, tight and eager.', None, 'A', None, 50, 6.2, 'I4', 4, 2.0, 1200, 6500, 8000, 8500, 280, 250, 'even', 'race', 'itb', False),
    ('engine_a_evo', 'Touring 2.0 Evo', 'Evolution heads and a lighter crank.', None, 'A', None, 53, 6.6, 'I4', 4, 2.0, 1200, 6800, 8300, 8800, 300, 262, 'even', 'race', 'itb', False),
    ('engine_a_works', 'Works 2.0', 'Individual throttle bodies: it inhales like a race car.', None, 'A', None, 56, 7.0, 'I4', 4, 2.0, 1300, 7200, 8700, 9200, 320, 270, 'even', 'race', 'itb', False),
    ('engine_s_v6', 'Race V6 3.0', 'Purpose-built race engine. Nothing is quiet any more.', None, 'S', None, 58, 7.5, 'V6', 6, 3.0, 1800, 7500, 9800, 10500, 480, 380, 'even', 'race', 'trumpets', False),
    ('engine_s_v8', 'Race V8 4.0', 'Flat-plane V8: a hard, ripping bark.', None, 'S', None, 62, 8.0, 'V8', 8, 4.0, 1500, 7000, 9200, 9800, 560, 470, 'flatplane', 'race', 'trumpets', False),
    ('engine_s_v10', 'Screamer V10 3.5', 'Thirteen and a half thousand revs. The sound of a grand prix.', None, 'S', None, 66, 8.5, 'V10', 10, 3.5, 2500, 10000, 12800, 13500, 650, 400, 'even', 'race', 'trumpets', False),
    ('engine_e_v6', 'Hybrid V6', 'Petrol and electric together: torque from nothing.', None, 'E', None, 62, 7.8, 'V6', 6, 2.4, 1500, 6500, 9500, 10500, 520, 480, 'even', 'race', 'trumpets', True),
    ('engine_e_v8', 'Hybrid V8', 'A crossplane V8 with a motor-generator on the crank.', None, 'E', None, 66, 8.3, 'V8', 8, 4.0, 1300, 6000, 8800, 9500, 600, 560, 'crossplane', 'race', 'trumpets', True),
    ('engine_e_12h', 'Twelve-hour hybrid V8', 'Built to run flat out from dusk to dawn.', None, 'E', None, 70, 8.8, 'V8', 8, 4.0, 1300, 6200, 9000, 9600, 650, 590, 'crossplane', 'race', 'trumpets', True),
    ('engine_e_economy', 'Economy hybrid', 'Sidegrade: gentler power, far further on a tank.', None, 'E', None, 64, 7.5, 'V6', 6, 1.6, 1200, 5500, 8500, 9500, 480, 450, 'even', 'race', 'trumpets', True),
]
ENGINE_BASE = 1500
for i, e in enumerate(ENGINES):
    (pid, name, hint, price, cls, shop, pl, acc, layout, cyl, litres, idle, tq, pw, red, hp, nm, firing, exhaust, intake, hybrid) = e
    if price is None:
        step = [x[0] for x in ENGINES if x[4] == cls].index(pid)
        price = ladder_price(ENGINE_BASE, cls, min(step, 2))
    add(pid, 'engine', name, hint, price, cls, shop, power_limit=pl, accel=acc, layout=layout, cylinders=cyl, litres=litres,
        idleRpm=idle, torqueRpm=tq, powerRpm=pw, redline=red, hp=hp, nm=nm, firing=firing, exhaust=exhaust, intake=intake, hybrid=hybrid,
        sound=ENGINE_SOUND.get(pid, ''))

# ---------------------------------------------------------------- induction
# (id, name, hint, price, class, shop, power x, torque x, spoolStart, spoolFull (fraction of redline), lag s, blowoff, antilag, electric)
TURBOS = [
    ('turbo_stock', 'Naturally aspirated', 'No forced induction. Predictable power.', 0, 'C', None, 1, 1, 0, 0, 0, False, False, False),
    ('turbo_street', 'Street turbo', 'A little snail: wait for it, then it pulls.', 700, 'C', None, 1.1, 1.05, 0.35, 0.6, 0.6, False, False, False),
    ('turbo_club', 'Club turbo', 'Stronger high-speed acceleration.', 1400, 'C', None, 1.18, 1.09, 0.38, 0.62, 0.5, True, False, False),
    ('turbo_sport', 'Sport turbo', 'Bigger compressor, sharper whistle.', 1900, 'C', None, 1.24, 1.12, 0.4, 0.64, 0.45, True, False, False),
    ('turbo_twin', 'Twin turbo', 'Two snails, one grin. Huge top-end power.', 2100, 'C', 'hillcrest', 1.3, 1.15, 0.3, 0.55, 0.35, True, False, False),
    ('turbo_b_single', 'Big-single turbo', 'A big turbo: laggy, then a shove.', None, 'B', None, 1.22, 1.08, 0.45, 0.7, 0.7, True, False, False),
    ('turbo_b_twinscroll', 'Twin-scroll turbo', 'Divided exhaust pulses spool it faster.', None, 'B', None, 1.28, 1.11, 0.35, 0.58, 0.35, True, False, False),
    ('turbo_b_twin', 'Tuner twin turbo', 'Two turbos, sequential: boost from low down.', None, 'B', None, 1.34, 1.14, 0.32, 0.55, 0.3, True, False, False),
    ('turbo_a_twinscroll', 'Touring twin-scroll', 'Tuned for circuit revs.', None, 'A', None, 1.22, 1.08, 0.35, 0.6, 0.3, True, False, False),
    ('turbo_a_works', 'Works turbo', 'Ball-bearing works unit.', None, 'A', None, 1.3, 1.11, 0.38, 0.62, 0.3, True, False, False),
    ('turbo_a_antilag', 'Anti-lag system', 'Keeps the turbo spinning off throttle. Bangs and crackles.', None, 'A', None, 1.34, 1.14, 0.2, 0.4, 0, True, True, False),
    ('turbo_s_race', 'Race turbo and intercooler', 'Cold, dense air.', None, 'S', None, 1.22, 1.08, 0.4, 0.65, 0.25, True, False, False),
    ('turbo_s_twin', 'Twin race turbos', 'One per bank.', None, 'S', None, 1.3, 1.11, 0.35, 0.6, 0.2, True, False, False),
    ('turbo_s_qualifying', 'Qualifying boost', 'Maximum boost for a flying lap.', None, 'S', None, 1.38, 1.14, 0.35, 0.6, 0.2, True, True, False),
    ('turbo_e_eturbo', 'E-turbo', 'An electric motor spins the turbo: no lag at all.', None, 'E', None, 1.18, 1.1, 0.1, 0.3, 0, False, False, True),
    ('turbo_e_ers', 'ERS pack', 'Stored energy, deployed out of every corner.', None, 'E', None, 1.22, 1.14, 0, 0.2, 0, False, False, True),
    ('turbo_e_twin', 'Twin e-turbo', 'Two electrically assisted turbos.', None, 'E', None, 1.3, 1.16, 0.1, 0.3, 0, False, False, True),
]
TURBO_BASE = 1900
for t in TURBOS:
    (pid, name, hint, price, cls, shop, pw, tq, s0, s1, lag, bov, al, electric) = t
    if price is None:
        step = [x[0] for x in TURBOS if x[4] == cls].index(pid)
        price = ladder_price(TURBO_BASE, cls, min(step, 2))
    add(pid, 'turbo', name, hint, price, cls, shop, power=pw, torque=tq, spoolStart=s0, spoolFull=s1, lag=lag, blowoff=bov, antilag=al, electric=electric)

# ---------------------------------------------------------------- gearboxes
# (id, name, hint, price, class, shop, gears, spread, shift s, type, top gear km/h per 1000 rpm, lsd)
# Fixed ratios (docs/47, gearing v2): top speed is whichever comes first, the engine's power against drag or the
# redline in top gear. A weak engine never reaches the top-gear limiter; a strong one needs a longer box.
BOXES = [
    ('transmission_stock', 'Stock 4-speed', 'Four long gears and a slow shift.', 0, 'C', None, 4, 3.6, 0.3, 'H', 16.1, False),
    ('transmission_close', 'Close-ratio 5-speed', 'Gears closer together: stays in the power.', 500, 'C', None, 5, 3.3, 0.28, 'H', 16.9, False),
    ('transmission_fivesport', 'Five-speed sport', 'A tighter linkage and quicker shifts.', 650, 'C', None, 5, 3.6, 0.22, 'H', 18.3, False),
    ('transmission_shortshift', 'Short-shift 5-speed', 'Short throws: shifts in a blink.', 1000, 'C', None, 5, 3.6, 0.16, 'H', 18.3, False),
    ('transmission_six', 'Six-speed box', 'Long legs without losing pull.', 1400, 'C', 'hillcrest', 6, 3.8, 0.2, 'H', 21.0, False),
    ('transmission_long', 'Long-ratio 5-speed', 'Sidegrade: tall gearing, relaxed revs at speed.', 800, 'C', None, 5, 4.0, 0.25, 'H', 22.2, False),
    ('transmission_b_six', 'Tuner six-speed', 'Six gears, tuner ratios.', None, 'B', None, 6, 3.7, 0.18, 'H', 22.5, False),
    ('transmission_b_shortsix', 'Short-shift six', 'Six gears and short throws.', None, 'B', None, 6, 3.7, 0.14, 'H', 22.5, False),
    ('transmission_b_lsd', 'LSD six-speed', 'A limited-slip differential: better drive out of corners.', None, 'B', None, 6, 3.7, 0.14, 'H', 22.5, True),
    ('transmission_a_close', 'Touring close six', 'Touring ratios, close and quick.', None, 'A', None, 6, 3.2, 0.12, 'H', 22.0, True),
    ('transmission_a_seq', 'Touring sequential', 'Sequential: pull back, push forward.', None, 'A', None, 6, 3.3, 0.08, 'S', 23.2, True),
    ('transmission_a_works', 'Works sequential', 'Dog engagement, flat shifts.', None, 'A', None, 6, 3.2, 0.06, 'S', 23.2, True),
    # S boxes (2 Oct, with the S platform's real drag): 21-22 km/h per 1000 rpm put the S V8 on its limiter at ~218 km/h
    # and the V6 at ~234 against drag-limited tops near 290 and 270; now 29-30 (285-315 km/h at the V8's and V6's redlines).
    ('transmission_s_dog', 'Sequential dog box', 'Brutal, instant, loud.', None, 'S', None, 6, 3.0, 0.06, 'S', 29.0, True),
    ('transmission_s_paddle', 'Paddle shift', 'Seven gears on paddles.', None, 'S', None, 7, 3.2, 0.04, 'P', 30.0, True),
    ('transmission_s_seamless', 'Seamless shift', 'Two gears engaged at once: no gap in the drive.', None, 'S', None, 7, 3.2, 0.015, 'SL', 30.0, True),
    # E boxes (docs/54, 2 Oct): geared for prototype top speeds (330+ km/h with the E platform's low drag); they were
    # 25.9/28.5 km/h per 1000 rpm, which put every E car on its limiter near 250-275 km/h.
    ('transmission_e_paddle', 'Endurance paddle', 'Seven gears, built to last.', None, 'E', None, 7, 3.3, 0.04, 'P', 33.0, True),
    ('transmission_e_seamless', 'Hybrid seamless', 'The e-motor fills every shift.', None, 'E', None, 7, 3.3, 0.015, 'SL', 33.0, True),
    ('transmission_e_12h', 'Twelve-hour box', 'Seamless and unbreakable.', None, 'E', None, 7, 3.3, 0.02, 'SL', 33.0, True),
    ('transmission_e_long', 'Low-drag long box', 'Sidegrade: tall top gear for long straights.', None, 'E', None, 7, 3.6, 0.04, 'P', 36.0, True),
]
BOX_BASE = 1000
for b in BOXES:
    (pid, name, hint, price, cls, shop, gears, spread, shift, kind, top, lsd) = b
    if price is None:
        step = [x[0] for x in BOXES if x[4] == cls].index(pid)
        price = ladder_price(BOX_BASE, cls, min(step, 2))
    # Legacy ceiling/pull until the real-gear drivetrain replaces them (docs/47 stage 2).
    legacy = {'transmission_stock': (25, 1.0), 'transmission_close': (23, 1.2), 'transmission_long': (34, .84), 'transmission_six': (38, 1.08),
              'transmission_fivesport': (28, 1.1), 'transmission_shortshift': (30, 1.12)}.get(pid, (90, 1.1))
    add(pid, 'transmission', name, hint, price, cls, shop, gears=gears, spread=spread, shiftTime=shift, shiftType=kind, kmhPer1000=top, lsd=lsd,
        ceiling=legacy[0], pull=legacy[1])

# ---------------------------------------------------------------- tyres, brakes, steering, frame
def simple(slot, base, rows):
    for r in rows:
        pid, name, hint, price, cls, shop, stats = r
        if price is None:
            same = [x for x in rows if x[4] == cls and x[3] is None]
            price = ladder_price(base, cls, min(same.index(r), 2))
        add(pid, slot, name, hint, price, cls, shop, **stats)

simple('tyres', 1050, [
    ('tyres_stock', 'Stock tyres', 'Basic road grip; poor on rough ground.', 0, 'C', None, {'road': 7, 'dirt': 3.5}),
    ('tyres_touring', 'Touring tyres', 'An affordable improvement on road and trail.', 350, 'C', None, {'road': 8, 'dirt': 5}),
    ('tyres_sport', 'Road sport tyres', 'Fast tarmac corners, weaker on loose ground.', 750, 'C', None, {'road': 9, 'dirt': 3}),
    ('tyres_clubsport', 'Club sport tyres', 'Soft shoulders and a sticky compound.', 1050, 'C', None, {'road': 9.6, 'dirt': 3.5}),
    ('tyres_grip', 'Hill grip tyres', 'Soft, sticky tarmac tyres for the twisty valley roads.', 1300, 'C', 'hillcrest', {'road': 10.2, 'dirt': 5}),
    ('tyres_trail', 'Trail tyres', 'Sidegrade: secure on dirt, less road grip.', 450, 'C', None, {'road': 7.5, 'dirt': 10}),
    ('tyres_rally', 'Rally tyres', 'Chunky all-rounders: fast on tarmac, brilliant off it.', 1100, 'C', 'hillcrest', {'road': 8.5, 'dirt': 12.5}),
    # Quarry Hills (docs/59): Grit & Gravel's own, released by gold in all six of Gwen's rally lessons.
    ('tyres_c_gravel', 'Gravel tyres', "Narrow, knobbly and loud: Quarry Hills' own. The best grip on loose ground in class C.", 1350, 'C', 'quarry', {'road': 7.5, 'dirt': 15, 'unlock': 'rallyschool'}),
    ('tyres_b_sport', 'Tuner sport tyres', 'A proper performance tyre.', None, 'B', None, {'road': 14.5, 'dirt': 4}),
    ('tyres_b_performance', 'Tuner performance tyres', 'Stiffer walls, sharper turn-in.', None, 'B', None, {'road': 15.5, 'dirt': 4}),
    ('tyres_b_semislick', 'Tuner semi-slick', 'Barely any tread: track days only.', None, 'B', None, {'road': 16.5, 'dirt': 3}),
    ('tyres_b_rally', 'Tarmac rally tyres', 'Sidegrade: gravel and tarmac alike.', 1600, 'B', None, {'road': 13.5, 'dirt': 12}),
    ('tyres_a_semislick', 'Touring semi-slick', 'Race rubber with a road-legal stamp.', None, 'A', None, {'road': 17.5, 'dirt': 3}),
    ('tyres_a_race', 'Touring race tyres', 'Full race compound.', None, 'A', None, {'road': 18.5, 'dirt': 3}),
    ('tyres_a_works', 'Works race tyres', 'The works team\'s compound.', None, 'A', None, {'road': 19.5, 'dirt': 3}),
    ('tyres_a_wet', 'Touring wet tyres', 'Sidegrade: grooved for rain.', 3000, 'A', None, {'road': 15, 'dirt': 6}),
    ('tyres_a_gravel', 'Touring gravel tyres', 'Sidegrade: for loose surfaces.', 3000, 'A', None, {'road': 13, 'dirt': 14}),
    ('tyres_s_hard', 'Hard slick', 'Lasts a race, a little less grip.', None, 'S', None, {'road': 20.5, 'dirt': 2.5}),
    ('tyres_s_medium', 'Medium slick', 'The all-round race tyre.', None, 'S', None, {'road': 21.5, 'dirt': 2.5}),
    ('tyres_s_soft', 'Soft slick', 'Maximum grip for a sprint.', None, 'S', None, {'road': 22.5, 'dirt': 2.5}),
    ('tyres_s_wet', 'Race wet', 'Sidegrade: full wets.', 6000, 'S', None, {'road': 17, 'dirt': 7}),
    ('tyres_e_hard', 'Endurance hard', 'Triple stints on one set.', None, 'E', None, {'road': 21, 'dirt': 2.5}),
    ('tyres_e_medium', 'Endurance medium', 'Double stints, more grip.', None, 'E', None, {'road': 22, 'dirt': 2.5}),
    ('tyres_e_qualifier', 'Qualifier', 'One perfect lap.', None, 'E', None, {'road': 23, 'dirt': 2.5}),
    ('tyres_e_night', 'Night and wet', 'Sidegrade: for the dark hours and the rain.', 10000, 'E', None, {'road': 18, 'dirt': 7}),
])
simple('brakes', 1250, [
    ('brakes_stock', 'Stock pads', 'Allow plenty of braking distance.', 0, 'C', None, {'braking': 14}),
    ('brakes_sport', 'Sport pads', 'Later braking; tyre grip still limits deceleration.', 300, 'C', None, {'braking': 19}),
    ('brakes_race', 'Race pads', 'Strong braking with matching road tyres.', 900, 'C', None, {'braking': 25}),
    ('brakes_bigbore', 'Big-bore front', 'Bigger discs and callipers up front.', 1250, 'C', None, {'braking': 28}),
    ('brakes_ceramic', 'Ceramic brakes', 'Stops on a coin, lap after lap.', 1500, 'C', 'hillcrest', {'braking': 30}),
    ('brakes_b_drilled', 'Drilled two-pot', 'Cross-drilled discs, two-piston callipers.', None, 'B', None, {'braking': 31}),
    ('brakes_b_braided', 'Braided big brake', 'Braided lines, a firm pedal.', None, 'B', None, {'braking': 33}),
    ('brakes_b_carbon', 'Carbon-metallic kit', 'Tuner\'s best.', None, 'B', None, {'braking': 35}),
    ('brakes_a_slotted', 'Slotted four-pot', 'Four pistons, slotted discs.', None, 'A', None, {'braking': 36}),
    ('brakes_a_big', 'Touring big brake', 'Huge discs behind the wheels.', None, 'A', None, {'braking': 38}),
    ('brakes_a_cooled', 'Cooled works brake', 'Ducted air keeps them fresh.', None, 'A', None, {'braking': 40}),
    ('brakes_s_sixpot', 'Six-pot monobloc', 'Race callipers.', None, 'S', None, {'braking': 42}),
    ('brakes_s_carbon', 'Carbon race brakes', 'Carbon on carbon.', None, 'S', None, {'braking': 45}),
    ('brakes_s_bbw', 'Brake-by-wire', 'Computer-balanced braking.', None, 'S', None, {'braking': 48}),
    ('brakes_e_ceramic', 'Carbon-ceramic', 'Endurance-proof.', None, 'E', None, {'braking': 44}),
    ('brakes_e_regen', 'Regen hybrid brakes', 'Braking charges the battery.', None, 'E', None, {'braking': 47}),
    ('brakes_e_12h', 'Twelve-hour package', 'Twelve hours without fade.', None, 'E', None, {'braking': 50}),
])
simple('steering', 1150, [
    ('steering_stock', 'Stock rack', 'Calm but slow response.', 0, 'C', None, {'response': 3, 'yaw': .88}),
    ('steering_quick', 'Quick rack', 'Quicker direction changes without adding grip.', 250, 'C', None, {'response': 4.5, 'yaw': 1.05}),
    ('steering_precision', 'Precision rack', 'Fast response for technical bends.', 850, 'C', None, {'response': 6, 'yaw': 1.18}),
    ('steering_sportrack', 'Sport rack and wheel', 'A small leather wheel and a quick rack.', 1150, 'C', None, {'response': 6.5, 'yaw': 1.23}),
    ('steering_rally', 'Rally rack', 'Sharp, eager turn-in for hairpins.', 1200, 'C', 'hillcrest', {'response': 7, 'yaw': 1.28}),
    ('steering_c_gravel', 'Gravel rack', 'Quick and light, for flicking the car into slides on the loose.', 1400, 'C', 'quarry', {'response': 7.2, 'yaw': 1.34, 'unlock': 'rallyschool'}),
    ('steering_b_quick', 'Quick-ratio rack', 'Two turns lock to lock.', None, 'B', None, {'response': 7, 'yaw': 1.3}),
    ('steering_b_rally', 'Tuner rally rack', 'Built for flicks.', None, 'B', None, {'response': 7.5, 'yaw': 1.34}),
    ('steering_b_wheel', 'Tuner wheel and rack', 'A suede three-spoke wheel.', None, 'B', None, {'response': 8, 'yaw': 1.38}),
    ('steering_a_rack', 'Touring rack', 'Direct and weighty.', None, 'A', None, {'response': 8.3, 'yaw': 1.4}),
    ('steering_a_quickrelease', 'Quick-release wheel', 'Race wheel on a quick-release hub.', None, 'A', None, {'response': 8.6, 'yaw': 1.42}),
    ('steering_a_works', 'Works rack', 'The works team\'s setup.', None, 'A', None, {'response': 9, 'yaw': 1.44}),
    ('steering_s_race', 'Race rack', 'No assistance, all feel.', None, 'S', None, {'response': 9.3, 'yaw': 1.46}),
    ('steering_s_paddle', 'Paddle wheel', 'Paddles behind the wheel.', None, 'S', None, {'response': 9.6, 'yaw': 1.48}),
    ('steering_s_assisted', 'Assisted race rack', 'Power-assisted precision.', None, 'S', None, {'response': 10, 'yaw': 1.5}),
    ('steering_e_display', 'Endurance display wheel', 'Screens and switches.', None, 'E', None, {'response': 9.3, 'yaw': 1.46}),
    ('steering_e_hybrid', 'Hybrid assist rack', 'Electric assistance.', None, 'E', None, {'response': 9.6, 'yaw': 1.48}),
    ('steering_e_night', 'Night-stint wheel', 'Easy on tired hands.', None, 'E', None, {'response': 10, 'yaw': 1.5}),
])
simple('chassis', 1400, [
    ('chassis_stock', 'Stock frame', "The standard car's weight.", 0, 'C', None, {'mass': 1.0}),
    ('chassis_light', 'Light frame', 'Better acceleration and braking from less weight.', 650, 'C', None, {'mass': 0.93}),
    ('chassis_seam', 'Seam-welded frame', 'Stiffer and lighter.', 1000, 'C', None, {'mass': 0.91}),
    ('chassis_stripped', 'Stripped shell', 'No carpets, no rear seats, no sound deadening.', 1400, 'C', None, {'mass': 0.89}),
    ('chassis_carbon', 'Carbon frame', 'Feather-light shell: quicker everywhere.', 1700, 'C', 'hillcrest', {'mass': 0.87}),
    ('chassis_b_strut', 'Strut-braced frame', 'Braces front and rear.', None, 'B', None, {'mass': .95}),
    ('chassis_b_halfcage', 'Half cage', 'A rear half-cage.', None, 'B', None, {'mass': .90}),
    ('chassis_b_carbon', 'Carbon panels', 'Carbon bonnet and boot.', None, 'B', None, {'mass': .86}),
    ('chassis_a_cage', 'Touring cage', 'A full welded cage.', None, 'A', None, {'mass': .95}),
    ('chassis_a_light', 'Lightweight touring', 'Thin glass, light panels.', None, 'A', None, {'mass': .90}),
    ('chassis_a_works', 'Works shell', 'Acid-dipped works body.', None, 'A', None, {'mass': .86}),
    ('chassis_s_tub', 'Race tub', 'Aluminium honeycomb.', None, 'S', None, {'mass': .95}),
    ('chassis_s_carbon', 'Carbon race tub', 'Carbon fibre monocoque.', None, 'S', None, {'mass': .90}),
    ('chassis_s_floor', 'Ground-effect floor', 'The floor sucks it to the road.', None, 'S', None, {'mass': .86}),
    ('chassis_e_mono', 'Endurance monocoque', 'Safe, stiff and light.', None, 'E', None, {'mass': .95}),
    ('chassis_e_hybrid', 'Hybrid carbon tub', 'Battery in the floor.', None, 'E', None, {'mass': .90}),
    ('chassis_e_lowdrag', 'Low-drag tub', 'Slippery for the long straights.', None, 'E', None, {'mass': .86}),
])

# ---------------------------------------------------------------- platforms (a class gate, not a fitted slot yet)
# drag: share of the street car's air drag (docs/54: the air feels it since 2 Oct; S .7 and E .6 for 300+ km/h).
# firm: suspension firmness, how much stiffer than the street car's springs; the body leans 1/(1+firm) as much
# (Richard, 2 Oct: the maximum build's nose lifted under power; race cars should feel glued down).
PLATFORMS = [
    {'id': 'platform_c', 'tier': 'C', 'name': 'Street platform', 'price': 0, 'kg': 850, 'drag': 1.0, 'downforce': 0, 'firm': 0},
    {'id': 'platform_b', 'tier': 'B', 'name': 'Tuner platform', 'price': 9100, 'kg': 880, 'drag': .95, 'downforce': 0, 'firm': .25},
    {'id': 'platform_a', 'tier': 'A', 'name': 'Touring platform', 'price': 18900, 'kg': 950, 'drag': .90, 'downforce': 0, 'firm': .65},
    {'id': 'platform_s', 'tier': 'S', 'name': 'Racing platform', 'price': 35000, 'kg': 780, 'drag': .70, 'downforce': .0025, 'firm': 2.3},  # m/s2 of push into the road per (m/s)^2 (docs/54: 1.2 g at 70 m/s)
    {'id': 'platform_e', 'tier': 'E', 'name': 'Endurance platform', 'price': 59500, 'kg': 850, 'drag': .60, 'downforce': .003, 'firm': 3},  # (1.5 g at 70 m/s: the raceway's crests threw the maximum build in the air without it)
]

data = {'schemaVersion': 2, 'source': 'tools/parts-ladder.py (docs/47)',
        'slots': ['engine', 'turbo', 'tyres', 'transmission', 'brakes', 'steering', 'chassis'],
        'parts': parts, 'platforms': PLATFORMS}
ids = [p['id'] for p in parts]
assert len(ids) == len(set(ids)), 'duplicate part id'
for slot in data['slots']:
    assert slot + '_stock' in ids, 'missing stock part ' + slot
with open(OUT, 'w', encoding='utf-8', newline='\n') as f:
    json.dump(data, f, indent=2, ensure_ascii=False)
    f.write('\n')
counts = {c: sum(1 for p in parts if p['tier'] == c) for c in CLASSES}
print('parts', len(parts), counts, 'platforms', len(PLATFORMS))
