namespace Rom
{
	using System;
	using MongoDB.Bson;
	using nvoid.db;
	using nvoid.db.Caching; 
	using Netlyt.Service.Donut;
	using Netlyt.Service.Integration;

    public class RomDonutContext : DonutContext
    {
		public CacheSet<string> f_0 { get; set; }
public CacheSet<string> f_1 { get; set; }
public CacheSet<string> f_2 { get; set; }
public CacheSet<string> f_3 { get; set; }
public CacheSet<string> f_4 { get; set; }
public CacheSet<string> f_5 { get; set; }
public CacheSet<string> f_6 { get; set; }
public CacheSet<string> f_7 { get; set; }
public CacheSet<string> f_8 { get; set; }
public CacheSet<string> f_9 { get; set; }
public CacheSet<string> f_10 { get; set; }
public CacheSet<string> f_11 { get; set; }
public CacheSet<string> f_12 { get; set; }
public CacheSet<string> f_13 { get; set; }
public CacheSet<string> f_14 { get; set; }
public CacheSet<string> f_15 { get; set; }
public CacheSet<string> f_16 { get; set; }
public CacheSet<string> f_17 { get; set; }
public CacheSet<string> f_18 { get; set; }
public CacheSet<string> f_19 { get; set; }
public CacheSet<string> f_20 { get; set; }
public CacheSet<string> f_21 { get; set; }
public CacheSet<string> f_22 { get; set; }
public CacheSet<string> f_23 { get; set; }
public CacheSet<string> f_24 { get; set; }
public CacheSet<string> f_25 { get; set; }
public CacheSet<string> f_26 { get; set; }
public CacheSet<string> f_27 { get; set; }
public CacheSet<string> f_28 { get; set; }
public CacheSet<string> f_29 { get; set; }
public CacheSet<string> f_30 { get; set; }
public CacheSet<string> f_31 { get; set; }
public CacheSet<string> f_32 { get; set; }
public CacheSet<string> f_33 { get; set; }
public CacheSet<string> f_34 { get; set; }
public CacheSet<string> f_35 { get; set; }
public CacheSet<string> f_36 { get; set; }
public CacheSet<string> f_37 { get; set; }
public CacheSet<string> f_38 { get; set; }
public CacheSet<string> f_39 { get; set; }
public CacheSet<string> f_40 { get; set; }
public CacheSet<string> f_41 { get; set; }
public CacheSet<string> f_42 { get; set; }
public CacheSet<string> f_43 { get; set; }
public CacheSet<string> f_44 { get; set; }
public CacheSet<string> f_45 { get; set; }
public CacheSet<string> f_46 { get; set; }
public CacheSet<string> f_47 { get; set; }
public CacheSet<string> f_48 { get; set; }
public CacheSet<string> f_49 { get; set; }
public CacheSet<string> f_50 { get; set; }
public CacheSet<string> f_51 { get; set; }
public CacheSet<string> f_52 { get; set; }
public CacheSet<string> f_53 { get; set; }
public CacheSet<string> f_54 { get; set; }


		[SourceFromIntegration("Rom")]
public DataSet<BsonDocument> Rom { get; set; }

		 
        public RomDonutContext(RedisCacher cacher, DataIntegration intd, IServiceProvider serviceProvider)
            : base(cacher, intd, serviceProvider)
        { 
        }

		protected override void ConfigureCacheMap()
        { 
			
        }

    }
}