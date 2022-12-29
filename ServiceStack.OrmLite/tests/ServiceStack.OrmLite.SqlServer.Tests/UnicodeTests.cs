using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using ServiceStack.DataAnnotations;
using ServiceStack.OrmLite.Converters;

namespace ServiceStack.OrmLite.SqlServerTests
{
    /// <summary>
    /// test for issue #69
    /// </summary>
    class UnicodeTests : OrmLiteTestBase
    {
        [Test]
        public void can_insert_and_retrieve_unicode_values()
        {
            //save and restore state, so it doesn't mess with other tests
            var stringConverter = OrmLiteConfig.DialectProvider.GetStringConverter();
            bool prevUnicodestate = stringConverter.UseUnicode;
            try {
                stringConverter.UseUnicode = true;

                var testData = new[]{
                "árvíztűrő tükörfúrógép",
                "ÁRVÍZTŰRŐ TÜKÖRFÚRÓGÉP", //these are the Hungarian "special" characters, they work fine out of the box. At least on Hungarian_Technical_CI_AS
                "♪♪♫",                    //this one comes back as 'ddd'
                //greek alphabet
                @"
Letter	Name	Sound value
Ancient[5]	Modern[6]
Α α	alpha	[a] [aː]	[a]
Β β	beta	[b]	[v]
Γ γ	gamma	[ɡ]	[ɣ] ~ [ʝ]
Δ δ	delta	[d]	[ð]
Ε ε	epsilon	[e]	[e]
Ζ ζ	zeta	[zd] (or [dz][7])	[z]
Η η	eta	[ɛː]	[i]
Θ θ	theta	[tʰ]	[θ]
Ι ι	iota	[i] [iː]	[i]
Κ κ	kappa	[k]	[k] ~ [c]
Λ λ	lambda	[l]	[l]
Μ μ	mu	[m]	[m]	
Letter	Name	Sound value
Ancient	Modern
Ν ν	nu	[n]	[n]
Ξ ξ	xi	[ks]	[ks]
Ο ο	omicron	[o]	[o]
Π π	pi	[p]	[p]
Ρ ρ	rho	[r]	[r]
Σ σς	sigma	[s]	[s]
Τ τ	tau	[t]	[t]
Υ υ	upsilon	[y] [yː]	[i]
Φ φ	phi	[pʰ]	[f]
Χ χ	chi	[kʰ]	[x] ~ [ç]
Ψ ψ	psi	[ps]	[ps]
Ω ω	omega	[ɔː]	[o]
"
            };

                using(var con = OpenDbConnection()) {
                    con.ExecuteSql(table_re_creation_script);

                    foreach(var item in testData) { con.Insert(new Unicode_poco { Text = item }); }

                    var fromDb = con.Select<Unicode_poco>().Select(x => x.Text).ToArray();

                    CollectionAssert.AreEquivalent(testData, fromDb);
                }
            }
            finally { stringConverter.UseUnicode = prevUnicodestate; }
        }


        /* *
--if you run this in SSMS, it produces 'ddd'
INSERT INTO [Unicode_poco] ([Text]) VALUES ('hai ♪♪♫')

--if you run this in SSMS, it works fine
INSERT INTO [Unicode_poco] ([Text]) VALUES (N'hai ♪♪♫')
           
select * from Unicode_poco
         * */


        private class Unicode_poco
        {
            [PrimaryKey, AutoIncrement]
            public int Id { get; set; }

            public string Text { get; set; }
        }

        /// <summary>
        /// because OrmLite does not create nvarchar columns
        /// </summary>
        private string table_re_creation_script = @"
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Unicode_poco]') AND type in (N'U'))
DROP TABLE [dbo].[Unicode_poco];


CREATE TABLE [dbo].[Unicode_poco](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Text] [nvarchar](4000) NULL,
 CONSTRAINT [PK_Unicode_poco] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]";
    }
}
