# Car Comparison Analysis: Balance for Street Racing

## Overview

Comparing **Toyota Altezza Hellspec** vs **Mitsubishi GTO Hellspec** for competitive balance on a street racing server.

---

## Key Specifications Comparison

| Parameter | Toyota Altezza | Mitsubishi GTO | Difference |
|-----------|---------------|----------------|------------|
| **Mass (kg)** | 1285 | 1350 | GTO +65 kg (+5%) |
| **Drivetrain** | RWD | AWD (45% front) | Different handling |
| **Gears** | 6-speed | 5-speed | Altezza more gears |
| **Final Drive** | 3.98 | 4.21 | GTO shorter (more accel) |
| **Rev Limiter** | 7500 RPM | 7500 RPM | Same |
| **Wheelbase** | 2.67m | 2.48m | Altezza longer |
| **Weight Dist.** | 50.5% front | 57% front | GTO more nose-heavy |

---

## Engine & Power

### Toyota Altezza Hellspec

**Power Curve (Torque in Nm):**
```
RPM    | Torque (Nm)
-------|------------
1000   | 228
2000   | 263
3000   | 307
4000   | 342
5000   | 360  <- PEAK
5500   | 355
6000   | 340
7000   | 300
```

**Turbo:**
- Max Boost: 1.15 (115% increase)
- Reference RPM: 3100 (where full boost is reached)
- Lag Up: 0.995 (slow spool)
- Lag Down: 0.991

**Estimated Peak Power:** ~360 Nm x 2.15 (1 + 1.15) = **774 Nm @ 5000 RPM with boost**
- Power: ~405 HP @ 5000 RPM (estimated)

### Mitsubishi GTO Hellspec

**Power Curve (Torque in Nm):**
```
RPM    | Torque (Nm)
-------|------------
1000   | 185
2000   | 208
3000   | 208
4000   | 270
4900   | 323  <- PEAK
5100   | 323
5500   | 314
6400   | 279
7000   | 247
```

**Turbo:**
- Max Boost: 1.31 (131% increase)
- Reference RPM: 2750 (earlier full boost)
- Lag Up: 0.9955 (similar spool)
- Lag Down: 0.99

**Estimated Peak Power:** ~323 Nm x 2.31 (1 + 1.31) = **746 Nm @ 4900 RPM with boost**
- Power: ~383 HP @ 4900 RPM (estimated)

### Power Analysis

| Metric | Altezza | GTO | Advantage |
|--------|---------|-----|-----------|
| Peak Torque (NA) | 360 Nm | 323 Nm | Altezza +11% |
| Max Boost | 1.15 | 1.31 | GTO +14% boost |
| Peak Torque (Boosted) | ~774 Nm | ~746 Nm | Altezza +4% |
| Boost Reference RPM | 3100 | 2750 | GTO earlier |
| Power/Weight Ratio | ~0.315 HP/kg | ~0.284 HP/kg | **Altezza +11%** |

**Key Insight:** The Altezza has better power-to-weight despite lower boost because of higher base torque and lower mass.

---

## Drivetrain

### Toyota Altezza (RWD, 6-Speed)

```
Gear Ratios:
1st: 3.290  -> Overall: 13.09 (3.29 x 3.98)
2nd: 1.901  -> Overall: 7.57
3rd: 1.450  -> Overall: 5.77
4th: 1.090  -> Overall: 4.34
5th: 0.902  -> Overall: 3.59
6th: 0.702  -> Overall: 2.79

Differential:
- Power Lock: 30%
- Coast Lock: 40%
- Preload: 20 Nm
```

### Mitsubishi GTO (AWD, 5-Speed)

```
Gear Ratios:
1st: 3.071  -> Overall: 12.93 (3.07 x 4.21)
2nd: 1.739  -> Overall: 7.32
3rd: 1.103  -> Overall: 4.64
4th: 0.823  -> Overall: 3.47
5th: 0.659  -> Overall: 2.77

AWD System:
- Front Share: 45%
- Centre Diff Power: 50% lock
- Centre Diff Coast: 50% lock
- Rear Diff Power: 25% lock
- Rear Diff Coast: 40% lock
```

### Drivetrain Analysis

| Factor | Altezza | GTO | Impact |
|--------|---------|-----|--------|
| 1st Gear Overall | 13.09 | 12.93 | Similar launch |
| Top Gear Overall | 2.79 | 2.77 | Similar top speed |
| Gear Spread | 4.69:1 | 4.66:1 | Similar |
| AWD Traction | No | Yes | **GTO huge advantage in launches** |
| Drivetrain Loss | ~15% | ~20-25% | Altezza more efficient |

**Key Insight:** The GTO's AWD gives it a massive traction advantage off the line, compensating for its weight disadvantage. However, it loses more power through the drivetrain.

---

## Brakes

| Parameter | Altezza | GTO | Difference |
|-----------|---------|-----|------------|
| Max Torque | 3250 Nm | 3950 Nm | GTO +22% |
| Front Bias | 72% | 72% | Same |
| Handbrake | 2000 Nm | 2100 Nm | Similar |

**Key Insight:** GTO has significantly stronger brakes, which compensates for its heavier weight for similar braking distances.

---

## Tyres

Both cars use "hellspec track" tyres with similar grip characteristics:

| Parameter | Altezza Front | Altezza Rear | GTO Front | GTO Rear |
|-----------|--------------|--------------|-----------|----------|
| Width | 280mm | 300mm | 275mm | 275mm |
| Radius | 312mm | 312mm | 324mm | 324mm |
| DY0 (Lateral Grip) | 1.6426 | 1.6614 | 1.6426 | 1.6614 |
| DX0 (Longitudinal) | 1.5866 | 1.6054 | 1.5866 | 1.6054 |

**Key Insight:** Tyres are nearly identical in grip coefficients. Altezza has slightly wider rear tyres (300mm vs 275mm) which helps its RWD traction.

---

## Aerodynamics

### Toyota Altezza
- Body drag: CD_GAIN = 1.0
- Diffuser: Yes, with speed-sensitive active aero
- 4 wing elements (body, front, rear, diffuser)

### Mitsubishi GTO
- Body drag: CD_GAIN = 1.75 **(75% more drag!)**
- Diffuser: Yes, dual diffuser setup
- 5 wing elements (body, front, rear, diffuser, diffuser_2)

**Key Insight:** The GTO has significantly more aerodynamic drag, limiting its top speed potential.

---

## Suspension

| Parameter | Altezza Front | Altezza Rear | GTO Front | GTO Rear |
|-----------|--------------|--------------|-----------|----------|
| Type | DWB | DWB | STRUT | DWB |
| Spring Rate | 110,763 N/m | 100,724 N/m | 88,290 N/m | 61,740 N/m |
| ARB | 21,500 Nm | 14,700 Nm | 46,000 Nm | 14,000 Nm |
| Static Camber | 0.4° | 2.2° | -2.6° | -2.51° |

**Key Insight:**
- Altezza has stiffer springs overall
- GTO has much stiffer front ARB (46,000 vs 21,500 Nm) which promotes understeer
- GTO has more aggressive camber settings

---

## Balance Assessment

### Current Advantages

**Toyota Altezza Advantages:**
1. Lighter by 65 kg
2. Better power-to-weight ratio (+11%)
3. Less aerodynamic drag
4. 6-speed gearbox (better power band utilization)
5. Wider rear tyres for RWD traction
6. More neutral weight distribution

**Mitsubishi GTO Advantages:**
1. AWD traction (huge for launches and exits)
2. Stronger brakes
3. Higher boost ceiling (1.31 vs 1.15)
4. Earlier boost onset (2750 vs 3100 RPM)
5. Shorter wheelbase (more agile)

### Predicted Performance

| Scenario | Likely Winner | Why |
|----------|---------------|-----|
| **Standing Start** | GTO | AWD traction advantage |
| **Rolling Start** | Altezza | Better power/weight |
| **Long Straight** | Altezza | Less drag, lighter |
| **Tight Corners** | GTO | AWD, shorter wheelbase |
| **High-Speed Corners** | Altezza | Better aero balance |
| **Braking Zones** | Even | GTO brakes compensate for weight |

---

## Recommendations for Balancing

### To Bring GTO Closer to Altezza Performance:

1. **Reduce Body Drag** (`aero.ini` WING_0)
   - Current: CD_GAIN=1.75
   - Suggested: CD_GAIN=1.2-1.3
   - Impact: Improves top speed, reduces straight-line deficit

2. **Reduce Mass** (`car.ini`)
   - Current: 1350 kg
   - Suggested: 1310-1320 kg
   - Impact: Better power-to-weight, more competitive

3. **OR Increase Boost** (`engine.ini` TURBO_0)
   - Current: MAX_BOOST=1.31
   - Suggested: MAX_BOOST=1.40
   - Impact: More power to compensate for weight/drag

### To Bring Altezza Closer to GTO Performance:

1. **Add Mass** (`car.ini`)
   - Current: 1285 kg
   - Suggested: 1310-1330 kg
   - Impact: Reduces power-to-weight advantage

2. **Reduce Boost** (`engine.ini` TURBO_0)
   - Current: MAX_BOOST=1.15
   - Suggested: MAX_BOOST=1.05-1.10
   - Impact: Less peak power

3. **Increase Drag** (`aero.ini` WING_0)
   - Current: CD_GAIN=1.0
   - Suggested: CD_GAIN=1.2
   - Impact: Lower top speed

### Suggested Balanced Configuration

For truly balanced racing where skill determines the outcome:

**Target:** Both cars should have similar:
- 0-100 km/h times (within 0.3s)
- Top speed (within 5 km/h)
- Lap times on reference track (within 0.5s)

**Quick Balance (Minimal Changes):**

| Car | Change | Value |
|-----|--------|-------|
| GTO | CD_GAIN (body) | 1.75 -> 1.3 |
| Altezza | TOTALMASS | 1285 -> 1315 |

This would:
- Give GTO better top speed (less drag)
- Give Altezza more weight (less acceleration advantage)
- Preserve each car's character (RWD vs AWD feel)

---

## Files to Modify for Balancing

### Power Adjustments
- `engine.ini` -> MAX_BOOST, WASTEGATE, REFERENCE_RPM
- `power.lut` -> Raw torque curve

### Weight Adjustments
- `car.ini` -> TOTALMASS

### Aerodynamic Adjustments
- `aero.ini` -> CD_GAIN, CL_GAIN per wing section

### Traction Adjustments
- `drivetrain.ini` -> Differential settings (POWER, COAST, PRELOAD)
- `tyres.ini` -> DY0, DX0 (grip coefficients)

### Gearing Adjustments
- `drivetrain.ini` -> GEAR_1 through GEAR_6, FINAL

---

## Next Steps

1. **Create a balancing tool** that can:
   - Parse these INI/LUT files
   - Calculate theoretical performance metrics
   - Suggest adjustments based on target balance
   - Export modified files

2. **Test in-game** with telemetry to validate:
   - Actual 0-100, 0-200 times
   - Top speed runs
   - Lap times on reference track

3. **Iterate** based on real-world testing data
