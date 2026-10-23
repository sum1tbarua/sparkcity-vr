# Spark City VR - Teacher Guide

Two short missions made of **7 hands-on mini-games** (about 14 minutes in total).
Every interaction is **point + trigger**. A bouncing yellow arrow shows where to look, a **Hint** button
helps when stuck (-20 points), and **? Learn more** gives a short explanation.
Each mini-game ends with a **REMEMBER** card: the one line students should memorize.

## The mini-games

| # | Mini-game | What students do | Concept |
|---|---|---|---|
| **Mission 1 - Flood Warning (batch, ~9 min)** ||||
| 1 | Data Sorter | 6 data sources slide in; KEEP or TOSS each, reading its 4 V's | 4 V's, choosing sources |
| 2 | Clean-Up Blaster | Dirty records fly toward them; zap each with the right tool before it arrives | dropDuplicates, dropna, cast, quarantine |
| 3 | Lazy Pipeline | Click blocks in order; nothing runs until the orange ACTION block | transformations vs actions, lazy evaluation |
| 4 | Balance the Cluster | Click driver / executor / partition; watch data skew stall the job; move partitions by hand | driver, executors, partitions, skew, repartition |
| 5 | Trust Check | Judge 2 practice reports, then their OWN pipeline's report (built from their earlier choices). The final call decides the city's fate | data trust, validating output |
| **Mission 2 - Rush Hour Stream (streaming, ~5 min)** ||||
| 6 | Window Catcher | Choose a watermark and run the stream until 95%+ of late events are caught with < 15 s delay | windows, late data, watermark trade-off |
| 7 | Crash Recovery | The streaming app crashes and restarts from 0%; pick the protection that makes it resume | checkpointing, fault tolerance |

## The REMEMBER lines (good for a review slide or exam)

1. Pick data with the 4 V's: Volume, Velocity, Variety, Veracity. Keep only what the decision NEEDS and what you can TRUST.
2. Duplicate -> dropDuplicates()   Missing -> dropna()   Wrong type -> cast()   Corrupted -> quarantine.
3. Transformations (filter, join, groupBy) are LAZY - they only build a plan. An ACTION (write, count, show, collect) makes Spark run the job.
4. Driver plans, executors work, one task per partition. Data skew = uneven partition sizes, so a few slow tasks hold up the whole stage. Repartitioning (df.repartition(16)) or salting a skewed key spreads the ROWS more evenly.
5. Before acting on data, check: right sources? cleaned? job finished? Good -> APPROVE. Bad data -> REJECT. Job not finished -> RERUN.
6. Streaming groups events into EVENT-TIME windows. A WATERMARK = how late events may be (latest event time minus the threshold). Events later than that may be dropped. Bigger watermark = more complete, but later results and more state. (The game's percentages are presets; window results are described for Append mode.)
7. A CHECKPOINT saves a stream's progress (offsets + state) to durable storage, so after a restart Spark RESUMES instead of starting over. Cache is not durable. (A single failed executor is simply retried.)

## Game flow

1. **Welcome space** (your artwork): ENTER THE GAME, TUTORIAL, or EXIT.
2. **Tutorial** (optional, 5 short cards) plays in the welcome space - Back / Next / Skip.
3. **Command Center - Mission Board:** choose Mission 1 or 2. Seated mode and recenter are set here.
4. **Mission briefing - Start:** only now does the Mission 1 flood countdown begin.
5. Mini-games, then the mission summary (Play again / Mission 2 / Mission board).

## Suggested 30-minute session (Nov 3)

| Time | Activity |
|---|---|
| 0-5 | Fit headsets; set Seated mode on the Mission Board if students sit |
| 5-8 | Optional **Tutorial** in the welcome space |
| 8-18 | **Mission 1** |
| 18-24 | **Mission 2** |
| 24-30 | Discussion using the REMEMBER lines |

With shared headsets, run in pairs: one plays, the other reads the panels aloud and advises, then swap.

## Answer key

- **Data Sorter** (need it AND trust it?): KEEP River Gauges, Weather Sensors, Emergency Calls, City Location Table. TOSS Social Entertainment Feed (31% veracity) and Power-Grid Logs (reliable, but not needed).
- **Clean-Up:** first an untimed practice record (duplicate). Then: two or three identical rows -> Remove Duplicates; null location -> Remove Missing; text "04" or "3.4" -> Fix Data Type (cast to int / double); -999 or ###ERR### -> Quarantine Corrupted.
- **Lazy Pipeline:** spark.read -> filter -> join -> groupBy + agg -> write. (collect and show are traps.) Check question: write.
- **Balance the Cluster:** click the pink core (driver), any tower (executor), the cube under the arrow (partition). Then move 6 partitions from Executor 2 until every executor has 4.
- **Trust Check:** Night-shift team -> REJECT; Backup team -> RERUN; **Your team** -> APPROVE only if there is no red [X] line and trust is 80+; otherwise REJECT (or RERUN), repair, rerun and judge again.
- **Window Catcher:** 10 minutes (99% caught, 9 s delay). 0/2/5 min drop too many events; 20 min is too slow.
- **Crash Recovery:** Add a checkpoint.

## Choices have consequences (Mission 1)

- A **flood countdown** (10 minutes) runs in the HUD corner and the water visibly rises on the city map. Each wrong answer costs 10 s, each hint 5 s. Timers pause while a help card is open.
- Choices stick: a tossed needed source is missing later, a kept unreliable source stays in the pipeline, and missed dirty records "slip through".
- The last Trust Check report is built from the student's own choices. **Critical problems** (an unreliable source kept, needed data tossed, a corrupted or wrong-type record missed) make APPROVE wrong no matter the trust score; smaller ones (duplicates, an unneeded but reliable source) only lower the score.
- If their own report is bad they can **REJECT -> REPAIR -> RERUN -> judge again**. Each repair costs 20 s and each rerun 25 s of flood time, so fixing mistakes is possible but has a price.
- The final call decides the city outcome: **SAFE**, **PARTLY FLOODED** or **FLOODED** (shown on the summary and logged).
- The flood clock waits during the Clean-Up practice record, and all timers and demos pause while a help card is open.

## Scoring

Points per mini-game, shown as 1-3 stars (3 stars = 85%+). Faster answers in Clean-Up give bonus points.
Wrong answers give no points but always show the correct answer, so every student finishes and sees the right model.

## Discussion prompts

- Which source was tempting because of its huge volume? Why did veracity matter more?
- In the Lazy Pipeline, the dashboard said "0 rows" until the action. Why does Spark wait?
- In the game you moved partition cubes by hand. What does df.repartition(16) really do to the rows - and why might one very skewed key still be a problem?
- Why did the whole stage wait for Executor 2? What would happen with 100 executors and one skewed key?
- A 20-minute watermark caught 100% of events. Why was it still the wrong choice for ambulances?
- Why can't cache save a stream after the app restarts, but a checkpoint can? Why is one crashed executor NOT a disaster?

