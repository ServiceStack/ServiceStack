using System;
using System.Linq;
using System.Collections.Generic;
using NUnit.Framework;

namespace ServiceStack.Text.Tests.UseCases
{
	[TestFixture]
	public class GMapDirectionsTests
	{
		static string json = @"{
   ""routes"" : [
      {
         ""bounds"" : {
            ""northeast"" : {
               ""lat"" : 33.375280,
               ""lng"" : -95.68744000000001
            },
            ""southwest"" : {
               ""lat"" : 30.261020,
               ""lng"" : -97.74461000000001
            }
         },
         ""copyrights"" : ""Map data ©2011 Google"",
         ""legs"" : [
            {
               ""distance"" : {
                  ""text"" : ""277 mi"",
                  ""value"" : 446262
               },
               ""duration"" : {
                  ""text"" : ""4 hours 56 mins"",
                  ""value"" : 17778
               },
               ""end_address"" : ""100-198 E Cooper Ave, Cooper, TX 75432, USA"",
               ""end_location"" : {
                  ""lat"" : 33.375280,
                  ""lng"" : -95.68744000000001
               },
               ""start_address"" : ""404-408 Colorado St, Austin, TX 78701, USA"",
               ""start_location"" : {
                  ""lat"" : 30.26712000000001,
                  ""lng"" : -97.74461000000001
               },
               ""steps"" : [
                  {
                     ""distance"" : {
                        ""text"" : ""25.4 mi"",
                        ""value"" : 40813
                     },
                     ""duration"" : {
                        ""text"" : ""35 mins"",
                        ""value"" : 2085
                     },
                     ""end_location"" : {
                        ""lat"" : 33.3720,
                        ""lng"" : -95.70700000000001
                     },
                     ""html_instructions"" : ""Keep \u003cb\u003eleft\u003c/b\u003e at the fork and merge onto \u003cb\u003eTX-24 N/TX-50 N/Farm to Market Rd 499\u003c/b\u003e\u003cdiv style=\""font-size:0.9em\""\u003eContinue to follow TX-24 N\u003c/div\u003e"",
                     ""polyline"" : {
                        ""levels"" : ""B??@????@?????@???@?????@??????????A??@?????@????@???@?@????@??????A?????@??@???@???@???@???A???@??@?@??@??@??????@?@????B"",
                        ""points"" : ""_oeiE~_|hQj@oCHkA@uDm@wCq@_BeB_Cg@UKe@qC}AiEiBwBs@wEiAqDmAiC}A_DaCkBmBwCeEmA{BcBqEa`@}qAcDqMgHe[mBcGuBsEqDmFsEiEiQaSoHaJsJeMaHkI}GmHoCmDeg@uh@aG}FeEeDmHwDmEyAmDw@wDi@kCOgMQmDSkGeA{EkBaCuAqDcD{JcMmBwBgB{AoD}BgDuA{E}AkGaDe^qYsE_DqFcC}DgAsN_C}DaAiCeAwCmBcX}S{EgEqLkJcD{BaCeAuBm@cEs@uBSwBE_NGol@?mGMkGg@wo@}HaGa@eGMm_@v@cDDcFGyi@oE_RsAwTmAeQiB{GoAmR}EcIcD_G}CaJyFaHuF_FaFgHiIsDgFcF_IuCwFoEsKiFmQ_U_aAqDyN}EgNsFiM}B_EqBqC}gAqsAmC_E}BeEaEmJ_My^}K}]yOke@yEoOue@wxAw{AstEmIwWy[uqAcEqLkk@qgAyx@o|A_GmLkmB{qDsPc\\"", ""<<=== here"": 0
                     },
                     ""start_location"" : {
                        ""lat"" : 33.128960,
                        ""lng"" : -95.995040
                     },
                     ""travel_mode"" : ""DRIVING""
                  }
               ],
               ""via_waypoint"" : []
            }
         ],
         ""overview_polyline"" : {
            ""levels"" : ""B@@A@@@@@A@@@A@@A@@@A@@A@@A@@A@@A@@@@A@@@A@A@@AA@@@@@A@@A@@@@B@@@A@@@@@@A@@@@A@@AA@A@@@A@AA@@@A@@@@A@@@@@B@@@A@@@@@A@@@@A@@@@@@@A@A@A@@AA@@@@@@@@@@@A@@@@A@@@A@@@@A@@@@@A@@@@@@@@@B"",
            ""points"" : ""opvwDxvqsQtD}EtPnEvMqu@qmBge@{tEedCoaBkPqViHiuG_oDkd@mMceAmLop@jRiTdAepHcv@mlFbgAemBrs@kvCbc@q{HfD{gCkg@meCf`@kTEcw@cSqy@cx@ajEyzBcdOc{DubDap@ouCkaA}tAiv@emI{xCadFscEomAqZe~AgOyx@mVgj@{f@osB_dCcs@gm@cjEahCasCegAih@ueAqp@y{BepCuoDsxAuvGiXgRurBml@qMqTkMoeAePuSmjOeoEipJ}tEspAw|@sz@eUqfCqpAyr@}w@kVmP_tAmd@}tGwy@orAqc@mdF}~BozKu{FclDqwAg}As_@kdBcy@c`AgFagAoe@ahBgGmsDyoA{_B~Eox@|ZoxAsJgy@Qk{AbBkt@bLupJhOcfFcNgrA{Ts|@}CsbEglAiaD_Xq\\~Ck_O~~Egi@F}tEgkCsxGiEql@oPyrDqaImwC}rFqtBcbFmgBelDgxDsgDieL_qEquSaiCoyAjFikAps@eyC|s@kkAg@wb@iO{rCovBssCcx@q`EopBuVkFuie@tEi\\zEwh@`[cVjFcaBeZejAkLoo@coA_TgGet@~AgdAyn@qQuXmMchAc\\ad@uFsv@e`@i[yTw[iEkWaDulDsQio@zBquCs`@geCokByeFefBopD_pC{cJ{n@s}@gT_g@wcD{gJeh@ihCukAepAgdAyuGye@w`BqaDieEgwAgsC}~@khD{C_aDyd@imBauEglJyqAkxAkfBmwAaiAyoBkqCwaDyjAqmCwLwGylAeSqWoLaW_YqWsz@m_@wYo\\cd@sDqZvNsmAxCojGbOkkAtCmdAac@mTuM_Oq|@{wC_`CahC}}@sOe[q[y~@em@kf@sMe~@op@oqAcBe`AgK{s@f@wdB{Mmm@oRkc@{_@}Vsd@mp@y`C{}AwuBm_Ew|L}a@g_BemFobKyIe`@GapAmHC?{F""
         },
         ""summary"" : ""I-35 N"",
         ""warnings"" : [],
         ""waypoint_order"" : []
      }
   ],
   ""status"" : ""OK""
}";
		[Test]
		public void Can_parse_Routes()
		{
			var doc = JsonObject.Parse(json);
			
			var routes = doc.ArrayObjects("routes");
			
			var route = routes[0];
			
			var overview_polyline = route.Object("overview_polyline");
			
			Console.WriteLine(overview_polyline.ToJson());
		}

		[Test]
		public void Can_parse_GMaps_directions_json ()
		{
			var results = JsonObject.Parse(json).ConvertTo(x=> new RouteResult
			{
				Status = x.Get("status") ?? "(null)",
				Routes = x.ArrayObjects("routes").ConvertAll(r=>new Route
				{
					Overview_Polyline = r.Object("overview_polyline").ConvertTo(p => 
						new Polyline
						{
							Points = p.Get("points")
						})
				})
			});
			
			Console.WriteLine("out: " + results.ToJson());
		}

		public class RouteResult
		{
			public string Status { get; set; }

			public List<Route> Routes { get; set; }
		}

		public class Route
		{
			public Polyline Overview_Polyline { get; set; }
		}

		public class Polyline
		{
			public string Points { get; set; }

			public string Levels { get; set; }	
		}

	}
}

