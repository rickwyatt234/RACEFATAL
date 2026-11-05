using System;
using System.Collections.Generic;
using RaceFatal.Shared;

namespace RaceFatal.Career
{
    // Content planning data only. Tracks still require real prefabs, forward/reverse
    // checkpoint graphs, AI routes and camera validation before they are race-ready.
    public static class ApprovedCareerBlueprint
    {
        public sealed class OpponentTeam
        {
            public string Id, Identity, SignatureBikeA, SignatureBikeB, ManufacturerA, ManufacturerB;
            public TeamPhilosophy Philosophy;
            public OpponentTeam(string id,string identity,TeamPhilosophy philosophy,string a,string b,string brandA,string brandB)
            { Id=id; Identity=identity; Philosophy=philosophy; SignatureBikeA=a; SignatureBikeB=b; ManufacturerA=brandA; ManufacturerB=brandB; }
        }
        public sealed class Track
        {
            public string Id, Name, Environment;
            public int Difficulty;
            public bool HasForwardAndReverse => true;
            public Track(string id,string name,string environment,int difficulty){Id=id;Name=name;Environment=environment;Difficulty=difficulty;}
        }
        public sealed class Utility
        {
            public string Id, Name, Node, Activation;
            public Utility(int index,string name,string node,string activation){Id="UTILITY_"+index.ToString("D2");Name=name;Node=node;Activation=activation;}
        }
        public sealed class ExclusiveVariant
        {
            public string Manufacturer, Technology;
            public ExclusiveVariant(string manufacturer,string technology){Manufacturer=manufacturer;Technology=technology;}
        }
        // 21 placeholders: teams retain two distinct signature bike models while equipment advances by class.
        public static readonly IReadOnlyList<OpponentTeam> Teams = new[]
        {
            new OpponentTeam("AG-01","Heavy Assault",TeamPhilosophy.Aggressive,"HARBINGER","CASTLE","Pace Industries","Leistung"),
            new OpponentTeam("AG-02","Pursuit Hunters",TeamPhilosophy.Aggressive,"KATANA","WIRE","One Kinetics","Kappa Engineering"),
            new OpponentTeam("AG-03","Close-Quarters Destroyers",TeamPhilosophy.Aggressive,"GOBLIN","DEMEANOR","Trident","One Kinetics"),
            new OpponentTeam("TW-01","Escort Formation",TeamPhilosophy.Teamwork,"CASTLE","SERAPH","Scudo Migliore","Hivemind Corp"),
            new OpponentTeam("TW-02","Coordinated Assault",TeamPhilosophy.Teamwork,"WIRE","HARBINGER","One Kinetics","Leistung"),
            new OpponentTeam("TW-03","Tactical Partners",TeamPhilosophy.Teamwork,"STELLA","KATANA","Monochrome","Lance Reba Group"),
            new OpponentTeam("RK-01","Reckless Speedsters",TeamPhilosophy.Risk,"CAFE","KATANA","Kappa Engineering","Catalyx"),
            new OpponentTeam("RK-02","Volatile Combatants",TeamPhilosophy.Risk,"DEMEANOR","HARBINGER","Leistung","Trident"),
            new OpponentTeam("RK-03","Last-Chance Racers",TeamPhilosophy.Risk,"GOBLIN","WIRE","Osaki Labs","Catalyx"),
            new OpponentTeam("BL-01","Professional Competitors",TeamPhilosophy.Balanced,"WIRE","STELLA","Pilion","Kappa Engineering"),
            new OpponentTeam("BL-02","Technical Generalists",TeamPhilosophy.Balanced,"SERAPH","KATANA","Hivemind Corp","Monochrome"),
            new OpponentTeam("BL-03","Championship Veterans",TeamPhilosophy.Balanced,"CASTLE","STELLA","Pace Industries","Scudo Migliore"),
            new OpponentTeam("OP-01","Scavengers",TeamPhilosophy.Opportunistic,"SERAPH","WIRE","Osaki Labs","One Kinetics"),
            new OpponentTeam("OP-02","Ambush Specialists",TeamPhilosophy.Opportunistic,"GOBLIN","DEMEANOR","Leistung","Lance Reba Group"),
            new OpponentTeam("OP-03","Tactical Predators",TeamPhilosophy.Opportunistic,"SERAPH","KATANA","Osaki Labs","Hivemind Corp"),
            new OpponentTeam("DF-01","Armored Survivors",TeamPhilosophy.Defensive,"CASTLE","HARBINGER","Pace Industries","Alion Limited"),
            new OpponentTeam("DF-02","Energy Conservators",TeamPhilosophy.Defensive,"STELLA","WIRE","Monochrome","Scudo Migliore"),
            new OpponentTeam("DF-03","Counterattack Specialists",TeamPhilosophy.Defensive,"CASTLE","GOBLIN","Alion Limited","Lance Reba Group"),
            new OpponentTeam("SP-01","Straight-Line Specialists",TeamPhilosophy.Speed,"KATANA","CAFE","Kappa Engineering","Leistung"),
            new OpponentTeam("SP-02","Precision Racers",TeamPhilosophy.Speed,"CAFE","SERAPH","Hivemind Corp","Kappa Engineering"),
            new OpponentTeam("SP-03","Boost Specialists",TeamPhilosophy.Speed,"KATANA","WIRE","Monochrome","Catalyx")
        };
        // Seven geographically distinct tracks; 14 directed layouts once authored.
        public static readonly IReadOnlyList<Track> Tracks = new[]
        {
            new Track("TRACK-01","Metropolis Circuit","Wide futuristic city streets and beginner-friendly flow",1),
            new Track("TRACK-02","Temperate Mountain Forest","Winding hills, forests, descents, elevation changes",3),
            new Track("TRACK-03","Ocean & Beach Circuit","Coastline, beach roads, ocean-side straights",2),
            new Track("TRACK-04","Southwestern Desert","American Southwest mesas, canyon roads, high speed",2),
            new Track("TRACK-05","Japanese-Inspired City","Dense neon city streets, elevated technical roads",3),
            new Track("TRACK-06","Underground Dark Cave","Caverns, tunnels and sharp vertical terrain",5),
            new Track("TRACK-07","Antarctica Snow Circuit","Polar glaciers, snowfields and icy cliffs",4)
        };
        // 19 approved utilities; these require specialized runtime behavior, not generic handling placeholders.
        public static readonly IReadOnlyList<Utility> Utilities = new[]
        {
            new Utility(1,"Targeting Jammer","Small","Passive"),
            new Utility(2,"Steering Actuator","Small","Passive"),
            new Utility(3,"Grip Stabilizer","Small","Passive"),
            new Utility(4,"Shield Recovery Controller","Small","Passive"),
            new Utility(5,"Missile Countermeasure System","Medium","Automatic"),
            new Utility(6,"Shield Rupture Capacitor","Medium","Reactive"),
            new Utility(7,"Shield Leech Field","Medium","Passive"),
            new Utility(8,"Gyroscopic Stabilizer","Medium","Passive"),
            new Utility(9,"Mass Reduction Assembly","Medium","Passive"),
            new Utility(10,"Structural Reinforcement","Medium","Passive"),
            new Utility(11,"Shield Regeneration Module","Medium","Passive"),
            new Utility(12,"Engine Optimization Module","Medium","Passive"),
            new Utility(13,"Energy Recovery Unit","Medium","Passive"),
            new Utility(14,"Booster Efficiency Module","Medium","Passive"),
            new Utility(15,"Magnetic Anchor","Medium","Passive"),
            new Utility(16,"Emergency Hardlight Projector","Large","ReactiveOncePerRace"),
            new Utility(17,"Plasma Siphon","Large","Manual"),
            new Utility(18,"Repulsion Generator","Large","Manual"),
            new Utility(19,"Damage-to-Energy Converter","Large","Passive")
        };
        public static readonly IReadOnlyList<ExclusiveVariant> Exclusives = new[]
        {
            new ExclusiveVariant("Catalyx","Atomic Prototype Engine"),
            new ExclusiveVariant("Pace Industries","Reinforced Non-Newtonian Chassis"),
            new ExclusiveVariant("Monochrome","High-Efficiency Ion Booster"),
            new ExclusiveVariant("One Kinetics","Precision Ballistic Railgun"),
            new ExclusiveVariant("Leistung","High-Yield Cluster Rocket"),
            new ExclusiveVariant("Osaki Labs","Prototype Particle Railgun"),
            new ExclusiveVariant("Snapdragon Incorporated","Adaptive Nanocomposite Chassis"),
            new ExclusiveVariant("Alion Limited","Experimental Ablative Shield")
        };
    }
}
