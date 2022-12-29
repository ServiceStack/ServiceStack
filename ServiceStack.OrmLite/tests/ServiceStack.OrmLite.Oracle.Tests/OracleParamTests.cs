using System;
using System.Collections.Generic;
//using System.ComponentModel.Composition;
//using System.ComponentModel.Composition.Hosting;
using System.Data;
using System.Linq;
using NUnit.Framework;
using ServiceStack.DataAnnotations;

namespace ServiceStack.OrmLite.Tests
{
    [TestFixture]
    public class OracleParamTests : OrmLiteTestBase
    {
        private void DropAndCreateTables(IDbConnection db)
        {
            if (db.TableExists("ParamRelBO"))
                db.DropTable<ParamRelBo>();

            db.CreateTable<ParamTestBo>(true);
            db.CreateTable<ParamRelBo>(true);
            db.CreateTable<ParamByteBo>(true);
            db.CreateTable<ParamLevelAsAlias>(true);
        }

        [Test]
        public void ORA_ParamTestInsert()
        {
            using (var db = OpenDbConnection())
            {
                DropAndCreateTables(db);
                var dateTimeNow =new DateTime( DateTime.Now.Year,  DateTime.Now.Month,  DateTime.Now.Day);

                db.Insert(new ParamTestBo { Id = 1, Double = 0.001, Int = 100, Info = "One", NullableBool = null, DateTime = dateTimeNow });
                db.Insert(new ParamTestBo { Id = 2, Double = 0.002, Int = 200, Info = "Two", NullableBool = true, DateTime = dateTimeNow });
                db.Insert(new ParamTestBo { Id = 3, Double = 0.003, Int = 300, Info = "Three", NullableBool = false, DateTime = dateTimeNow.AddDays(23) });
                db.Insert(new ParamTestBo { Id = 4, Double = 0.004, Int = 400, Info = "Four", NullableBool = null });
                db.Insert(new ParamTestBo { Id = 5, Double = 0.005, Int = 500, Info = "Five", NullableBool = null, UInt = uint.MaxValue});

                var bo1 = db.Select<ParamTestBo>(q => q.Id == 1).Single();
                var bo2 = db.Select<ParamTestBo>(q => q.Id == 2).Single();
                var bo3 = db.Select<ParamTestBo>(q => q.Id == 3).Single();
                var bo4 = db.Select<ParamTestBo>(q => q.Id == 4).Single();
                var bo5 = db.Select<ParamTestBo>(q => q.Id == 5).Single();

                Assert.AreEqual(1, bo1.Id);
                Assert.AreEqual(2, bo2.Id);
                Assert.AreEqual(3, bo3.Id);
                Assert.AreEqual(4, bo4.Id);
                Assert.AreEqual(5, bo5.Id);

                Assert.AreEqual(0.001, bo1.Double);
                Assert.AreEqual(0.002, bo2.Double);
                Assert.AreEqual(0.003, bo3.Double);
                Assert.AreEqual(0.004, bo4.Double);
                Assert.AreEqual(0.005, bo5.Double);

                Assert.AreEqual(100, bo1.Int);
                Assert.AreEqual(200, bo2.Int);
                Assert.AreEqual(300, bo3.Int);
                Assert.AreEqual(400, bo4.Int);
                Assert.AreEqual(500, bo5.Int);

                Assert.AreEqual("One", bo1.Info);
                Assert.AreEqual("Two", bo2.Info);
                Assert.AreEqual("Three", bo3.Info);
                Assert.AreEqual("Four", bo4.Info);
                Assert.AreEqual("Five", bo5.Info);

                Assert.AreEqual(null, bo1.NullableBool);
                Assert.AreEqual(true, bo2.NullableBool);
                Assert.AreEqual(false, bo3.NullableBool);
                Assert.AreEqual(null, bo4.NullableBool);
                Assert.AreEqual(null, bo5.NullableBool);

                Assert.AreEqual(dateTimeNow, bo1.DateTime);
                Assert.AreEqual(dateTimeNow, bo2.DateTime);
                Assert.AreEqual(dateTimeNow.AddDays(23), bo3.DateTime);
                Assert.AreEqual(null, bo4.DateTime);
                Assert.AreEqual(null, bo5.DateTime);

                Assert.AreEqual(null, bo1.UInt);
                Assert.AreEqual(null, bo2.UInt);
                Assert.AreEqual(null, bo3.UInt);
                Assert.AreEqual(null, bo4.UInt);
                Assert.AreEqual(uint.MaxValue, bo5.UInt);
            }
        }

        [Test]
        public void ORA_ParamTestUpdate()
        {
            using (var db = OpenDbConnection())
            {
                DropAndCreateTables(db);

                var bo1 = new ParamTestBo { Id = 1, Double = 0.001, Int = 100, Info = "One", NullableBool = true };
                var bo2 = new ParamTestBo { Id = 2, Double = 0.002, Int = 200, Info = "Two", NullableBool = true, DateTime = DateTime.Now };
                db.Insert(bo1);
                db.Insert(bo2);

                bo1.Double = 0.01;
                bo1.Int = 10000;
                bo1.Info = "OneUpdated";
                bo1.NullableBool = null;
                bo1.DateTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day);

                db.Update(bo1);

                var bo1Check = db.SingleById<ParamTestBo>(1);

                Assert.AreEqual(bo1.Double, bo1Check.Double);
                Assert.AreEqual(bo1.Int, bo1Check.Int);
                Assert.AreEqual(bo1.Info, bo1Check.Info);
                Assert.AreEqual(bo1.DateTime, bo1Check.DateTime);


                Assert.GreaterOrEqual(DateTime.Now, bo2.DateTime);

                bo2.Info = "TwoUpdated";
                bo2.Int = 9923;
                bo2.NullableBool = false;
                bo2.DateTime = DateTime.Now.AddDays(10);

                db.Update(bo2);

                var bo2Check = db.SingleById<ParamTestBo>(2);

                Assert.Less(DateTime.Now, bo2.DateTime);
                Assert.AreEqual("TwoUpdated", bo2Check.Info);
                Assert.AreEqual(9923, bo2Check.Int);
                Assert.AreEqual(false, bo2Check.NullableBool);
            }
        }

        [Test]
        public void ORA_ParamTestDelete()
        {
            using (var db = OpenDbConnection())
            {
                DropAndCreateTables(db);

                db.Insert(new ParamTestBo { Id = 1 });
                db.Insert(new ParamTestBo { Id = 2 });
                db.Insert(new ParamTestBo { Id = 3 });

                Assert.IsNotNull(db.Select<ParamTestBo>(q => q.Id == 1).FirstOrDefault());
                Assert.IsNotNull(db.Select<ParamTestBo>(q => q.Id == 2).FirstOrDefault());
                Assert.IsNotNull(db.Select<ParamTestBo>(q => q.Id == 3).FirstOrDefault());

                db.DeleteById<ParamTestBo>(1);
                db.DeleteById<ParamTestBo>(2);
                db.DeleteById<ParamTestBo>(3);

                Assert.IsNull(db.Select<ParamTestBo>(q => q.Id == 1).FirstOrDefault());
                Assert.IsNull(db.Select<ParamTestBo>(q => q.Id == 2).FirstOrDefault());
                Assert.IsNull(db.Select<ParamTestBo>(q => q.Id == 3).FirstOrDefault());
            }
        }

        [Test]
        public void ORA_ParamTestGetById()
        {
            using (var db = OpenDbConnection())
            {
                DropAndCreateTables(db);

                db.Insert(new ParamTestBo { Id = 1, Info = "Item1" });
                db.Insert(new ParamTestBo { Id = 2, Info = "Item2" });
                db.Insert(new ParamTestBo { Id = 3, Info = "Item3" });

                Assert.AreEqual("Item1", db.SingleById<ParamTestBo>(1).Info);
                Assert.AreEqual("Item2", db.SingleById<ParamTestBo>(2).Info);
                Assert.AreEqual("Item3", db.SingleById<ParamTestBo>(3).Info);
            }
        }

        [Test]
        public void ORA_ParamTestSelectLambda()
        {
            using (var db = OpenDbConnection())
            {
                DropAndCreateTables(db);

                LoadParamTestBo(db);

                //select multiple items
                Assert.AreEqual(3, db.Select<ParamTestBo>(q => q.NullableBool == null).Count);
                Assert.AreEqual(1, db.Select<ParamTestBo>(q => q.NullableBool == true).Count);
                Assert.AreEqual(1, db.Select<ParamTestBo>(q => q.NullableBool == false).Count);
                Assert.AreEqual(1, db.Select<ParamTestBo>(q => q.UInt == uint.MaxValue).Count);
                Assert.AreEqual(4, db.Select<ParamTestBo>(q => q.UInt == null).Count);

                Assert.AreEqual(1, db.Select<ParamTestBo>(q => q.Info == "Two").Count);
                Assert.AreEqual(1, db.Select<ParamTestBo>(q => q.Int == 300).Count);
                Assert.AreEqual(1, db.Select<ParamTestBo>(q => q.Double == 0.003).Count);
            }
        }

        private void LoadParamTestBo(IDbConnection db)
        {
            db.Insert(new ParamTestBo { Id = 1, Double = 0.001, Int = 100, Info = "One", NullableBool = null });
            db.Insert(new ParamTestBo { Id = 2, Double = 0.002, Int = 200, Info = "Two", NullableBool = true });
            db.Insert(new ParamTestBo { Id = 3, Double = 0.003, Int = 300, Info = "Three", NullableBool = false });
            db.Insert(new ParamTestBo { Id = 4, Double = 0.004, Int = 400, Info = "Four", NullableBool = null });
            db.Insert(new ParamTestBo { Id = 5, Double = 0.005, Int = 500, Info = "Five", NullableBool = null, UInt = uint.MaxValue});
        }

        [Test]
        public void ORA_ParamTestSelectLambda2()
        {
            using (var db = OpenDbConnection())
            {
                DropAndCreateTables(db);

                LoadParamTestBo(db);
                LoadParamRelBo(db);

                Assert.AreEqual(8, db.Select<ParamRelBo>(q => q.Info == "T1").Count);
                Assert.AreEqual(2, db.Select<ParamRelBo>(q => q.Info == "T2").Count);
               
                Assert.AreEqual(3, db.Select<ParamRelBo>(q => q.Info == "T1" && (q.PtId == 2 || q.PtId == 3) ).Count);
            }
        }

        private void LoadParamRelBo(IDbConnection db)
        {
            db.Insert(new ParamRelBo { PtId = 1, Info = "T1" });
            db.Insert(new ParamRelBo { PtId = 1, Info = "T1" });
            db.Insert(new ParamRelBo { PtId = 1, Info = "T1" });
            db.Insert(new ParamRelBo { PtId = 1, Info = "T1" });
            db.Insert(new ParamRelBo { PtId = 2, Info = "T1" });
            db.Insert(new ParamRelBo { PtId = 2, Info = "T1" });
            db.Insert(new ParamRelBo { PtId = 3, Info = "T1" });
            db.Insert(new ParamRelBo { PtId = 4, Info = "T1" });
            db.Insert(new ParamRelBo { PtId = 3, Info = "T2" });
            db.Insert(new ParamRelBo { PtId = 4, Info = "T2" });
        }

        [Test]
        public void ORA_ParamTestSelectLambdaComplex()
        {
            using (var db = OpenDbConnection())
            {
                DropAndCreateTables(db);

                LoadParamTestBo(db);
                LoadParamRelBo(db);

                Assert.AreEqual(10, db.Select<ParamRelBo>(q => Sql.In(q.Info, "T1", "T2")).Count);
                Assert.AreEqual(10, db.Select<ParamRelBo>(q => q.Info.StartsWith("T")).Count);
                Assert.AreEqual(8, db.Select<ParamRelBo>(q => q.Info.EndsWith("1")).Count);
                Assert.AreEqual(10, db.Select<ParamRelBo>(q => q.Info.Contains("T")).Count);
            }
        }

        [Test]
        public void ORA_ParamByteTest()
        {
            using (var db = OpenDbConnection())
            {
                DropAndCreateTables(db);

                db.DeleteAll<ParamByteBo>();
                var bo1 = new ParamByteBo { Id = 1, Data = new byte[] { 1, 25, 43, 3, 1, 66, 82, 23, 11, 44, 66, 22, 52, 62, 76, 19, 30, 91, 4 } };

                db.Insert(bo1);
                var bo1Check = db.Select<ParamByteBo>(s => s.Id == bo1.Id).Single();

                Assert.AreEqual(bo1.Id, bo1Check.Id);
                Assert.AreEqual(bo1.Data, bo1Check.Data);

                db.DeleteAll<ParamByteBo>();
            }
        }

        [Test]
        public void ORA_ReservedNameLevelAsAlias_Test()
        {
            using (var db = OpenDbConnection())
            {
                DropAndCreateTables(db);

                var row = new ParamLevelAsAlias { Id = 2, ApprovalLevel = 212218 };
                db.Insert(row);

                row.ApprovalLevel = 676;
                db.Update(row);
            }
        }

        //[ImportMany(typeof(IParam))]
// ReSharper disable UnassignedField.Compiler
        private IEnumerable<IParam> _reservedNameParams => typeof(IParam).Assembly.GetExportedTypes()
            .Where(x => x.HasInterface(typeof(IParam))).Select(x => Activator.CreateInstance(x) as IParam).ToArray();
// ReSharper restore UnassignedField.Compiler

        [Test]
        public void ORA_ReservedNames_Test()
        {
            ResolveReservedNameParameters();
            using (var db = OpenDbConnection())
            {
                foreach (var param in _reservedNameParams)
                {
                    try
                    {
                        db.CreateTable(true, param.GetType());
                        param.Id = 123;
                        param.SetValue(1000);
                        param.InsertDb(db);
                        param.SetValue(2343);
                        param.UpdateDb(db);
                    }
                    catch (Exception ex)
                    {
                        throw new Exception(
                            string.Format("Unable to process Oracle parameter of type: {0}", param.GetType()), ex);
                    }
                }
            }
        }

        private void ResolveReservedNameParameters()
        {
//            var catalog = new AssemblyCatalog(typeof(IParam).Assembly);
//            var container = new CompositionContainer(catalog);
//            container.ComposeParts(this);
        }

        //[InheritedExport]
        public interface IParam
        {
            int Id { get; set; }
            void InsertDb(IDbConnection db);
            void UpdateDb(IDbConnection db);
            void SetValue(int value);
        }

        public abstract class Param<T>: IParam
        {
            public int Id { get; set; }
            public abstract void SetValue(int value);
            public abstract T GetObject();
            public void InsertDb(IDbConnection db) { db.Insert(GetObject()); }
            public void UpdateDb(IDbConnection db) { db.Update(GetObject()); }
        }

        public class ParamComment : Param<ParamComment>
        {
            public int Comment { get; set; }
            public override void SetValue(int value) { Comment = value; }
            public override ParamComment GetObject() { return this; }
        }

        public class ParamOrder : Param<ParamOrder>
        {
            public int Order { get; set; }
            public override void SetValue(int value) { Order = value; }
            public override ParamOrder GetObject() { return this; }
        }

        public class ParamLeft : Param<ParamLeft>
        {
            public int Left { get; set; }
            public override void SetValue(int value) { Left = value; }
            public override ParamLeft GetObject() { return this; }
        }

        public class ParamUser : Param<ParamUser>
        {
            public int User { get; set; }
            public override void SetValue(int value) { User = value; }
            public override ParamUser GetObject() { return this; }
        }

        public class ParamPassword : Param<ParamPassword>
        {
            public int Password { get; set; }
            public override void SetValue(int value) { Password = value; }
            public override ParamPassword GetObject() { return this; }
        }

        public class ParamActive : Param<ParamActive>
        {
            public int Active { get; set; }
            public override void SetValue(int value) { Active = value; }
            public override ParamActive GetObject() { return this; }
        }

        public class ParamDouble : Param<ParamDouble>
        {
            public int Double { get; set; }
            public override void SetValue(int value) { Double = value; }
            public override ParamDouble GetObject() { return this; }
        }

        public class ParamFloat : Param<ParamFloat>
        {
            public int Float { get; set; }
            public override void SetValue(int value) { Float = value; }
            public override ParamFloat GetObject() { return this; }
        }

        public class ParamDecimal : Param<ParamDecimal>
        {
            public int Decimal { get; set; }
            public override void SetValue(int value) { Decimal = value; }
            public override ParamDecimal GetObject() { return this; }
        }

        public class ParamString : Param<ParamString>
        {
            public int String { get; set; }
            public override void SetValue(int value) { String = value; }
            public override ParamString GetObject() { return this; }
        }

        public class ParamDate : Param<ParamDate>
        {
            public int Date { get; set; }
            public override void SetValue(int value) { Date = value; }
            public override ParamDate GetObject() { return this; }
        }

        public class ParamDateTime : Param<ParamDateTime>
        {
            public int DateTime { get; set; }
            public override void SetValue(int value) { DateTime = value; }
            public override ParamDateTime GetObject() { return this; }
        }

        public class ParamType : Param<ParamType>
        {
            public int Type { get; set; }
            public override void SetValue(int value) { Type = value; }
            public override ParamType GetObject() { return this; }
        }

        public class ParamTimestamp : Param<ParamTimestamp>
        {
            public int Timestamp { get; set; }
            public override void SetValue(int value) { Timestamp = value; }
            public override ParamTimestamp GetObject() { return this; }
        }

        public class ParamIndex : Param<ParamIndex>
        {
            public int Index { get; set; }
            public override void SetValue(int value) { Index = value; }
            public override ParamIndex GetObject() { return this; }
        }

        public class ParamAccess : Param<ParamAccess>
        {
            public int Access { get; set; }
            public override void SetValue(int value) { Access = value; }
            public override ParamAccess GetObject() { return this; }
        }

        public class ParamDefault : Param<ParamDefault>
        {
            public int Default { get; set; }
            public override void SetValue(int value) { Default = value; }
            public override ParamDefault GetObject() { return this; }
        }

        public class ParamInteger : Param<ParamInteger>
        {
            public int Integer { get; set; }
            public override void SetValue(int value) { Integer = value; }
            public override ParamInteger GetObject() { return this; }
        }

        public class ParamOnline : Param<ParamOnline>
        {
            public int Online { get; set; }
            public override void SetValue(int value) { Online = value; }
            public override ParamOnline GetObject() { return this; }
        }

        public class ParamStart : Param<ParamStart>
        {
            public int Start { get; set; }
            public override void SetValue(int value) { Start = value; }
            public override ParamStart GetObject() { return this; }
        }

        public class ParamAdd : Param<ParamAdd>
        {
            public int Add { get; set; }
            public override void SetValue(int value) { Add = value; }
            public override ParamAdd GetObject() { return this; }
        }

        public class ParamDelete : Param<ParamDelete>
        {
            public int Delete { get; set; }
            public override void SetValue(int value) { Delete = value; }
            public override ParamDelete GetObject() { return this; }
        }

        public class ParamIntersect : Param<ParamIntersect>
        {
            public int Intersect { get; set; }
            public override void SetValue(int value) { Intersect = value; }
            public override ParamIntersect GetObject() { return this; }
        }

        public class ParamOption : Param<ParamOption>
        {
            public int Option { get; set; }
            public override void SetValue(int value) { Option = value; }
            public override ParamOption GetObject() { return this; }
        }

        public class ParamSuccessful : Param<ParamSuccessful>
        {
            public int Successful { get; set; }
            public override void SetValue(int value) { Successful = value; }
            public override ParamSuccessful GetObject() { return this; }
        }

        public class ParamDesc : Param<ParamDesc>
        {
            public int Desc { get; set; }
            public override void SetValue(int value) { Desc = value; }
            public override ParamDesc GetObject() { return this; }
        }

        public class ParamAll : Param<ParamAll>
        {
            public int All { get; set; }
            public override void SetValue(int value) { All = value; }
            public override ParamAll GetObject() { return this; }
        }

        public class ParamInto : Param<ParamInto>
        {
            public int Into { get; set; }
            public override void SetValue(int value) { Into = value; }
            public override ParamInto GetObject() { return this; }
        }

        public class ParamLevel : Param<ParamLevel>
        {
            public int Level { get; set; }
            public override void SetValue(int value) { Level = value; }
            public override ParamLevel GetObject() { return this; }
        }

        public class ParamOr : Param<ParamOr>
        {
            public int Or { get; set; }
            public override void SetValue(int value) { Or = value; }
            public override ParamOr GetObject() { return this; }
        }

        public class ParamSynonym : Param<ParamSynonym>
        {
            public int Synonym { get; set; }
            public override void SetValue(int value) { Synonym = value; }
            public override ParamSynonym GetObject() { return this; }
        }

        public class ParamAlter : Param<ParamAlter>
        {
            public int Alter { get; set; }
            public override void SetValue(int value) { Alter = value; }
            public override ParamAlter GetObject() { return this; }
        }

        public class ParamDistinct : Param<ParamDistinct>
        {
            public int Distinct { get; set; }
            public override void SetValue(int value) { Distinct = value; }
            public override ParamDistinct GetObject() { return this; }
        }

        public class ParamIs : Param<ParamIs>
        {
            public int Is { get; set; }
            public override void SetValue(int value) { Is = value; }
            public override ParamIs GetObject() { return this; }
        }

        public class ParamSysdate : Param<ParamSysdate>
        {
            public int Sysdate { get; set; }
            public override void SetValue(int value) { Sysdate = value; }
            public override ParamSysdate GetObject() { return this; }
        }

        public class ParamAnd : Param<ParamAnd>
        {
            public int And { get; set; }
            public override void SetValue(int value) { And = value; }
            public override ParamAnd GetObject() { return this; }
        }

        public class ParamDrop : Param<ParamDrop>
        {
            public int Drop { get; set; }
            public override void SetValue(int value) { Drop = value; }
            public override ParamDrop GetObject() { return this; }
        }

        public class ParamPctFree : Param<ParamPctFree>
        {
            public int PctFree { get; set; }
            public override void SetValue(int value) { PctFree = value; }
            public override ParamPctFree GetObject() { return this; }
        }

        public class ParamTable : Param<ParamTable>
        {
            public int Table { get; set; }
            public override void SetValue(int value) { Table = value; }
            public override ParamTable GetObject() { return this; }
        }

        public class ParamAny : Param<ParamAny>
        {
            public int Any { get; set; }
            public override void SetValue(int value) { Any = value; }
            public override ParamAny GetObject() { return this; }
        }

        public class ParamElse : Param<ParamElse>
        {
            public int Else { get; set; }
            public override void SetValue(int value) { Else = value; }
            public override ParamElse GetObject() { return this; }
        }

        public class ParamLike : Param<ParamLike>
        {
            public int Like { get; set; }
            public override void SetValue(int value) { Like = value; }
            public override ParamLike GetObject() { return this; }
        }

        public class ParamPrior : Param<ParamPrior>
        {
            public int Prior { get; set; }
            public override void SetValue(int value) { Prior = value; }
            public override ParamPrior GetObject() { return this; }
        }

        public class ParamThen : Param<ParamThen>
        {
            public int Then { get; set; }
            public override void SetValue(int value) { Then = value; }
            public override ParamThen GetObject() { return this; }
        }

        public class ParamAs : Param<ParamAs>
        {
            public int As { get; set; }
            public override void SetValue(int value) { As = value; }
            public override ParamAs GetObject() { return this; }
        }

        public class ParamExclusive : Param<ParamExclusive>
        {
            public int Exclusive { get; set; }
            public override void SetValue(int value) { Exclusive = value; }
            public override ParamExclusive GetObject() { return this; }
        }

        public class ParamLock : Param<ParamLock>
        {
            public int Lock { get; set; }
            public override void SetValue(int value) { Lock = value; }
            public override ParamLock GetObject() { return this; }
        }

        public class ParamPrivileges : Param<ParamPrivileges>
        {
            public int Privileges { get; set; }
            public override void SetValue(int value) { Privileges = value; }
            public override ParamPrivileges GetObject() { return this; }
        }

        public class ParamTo : Param<ParamTo>
        {
            public int To { get; set; }
            public override void SetValue(int value) { To = value; }
            public override ParamTo GetObject() { return this; }
        }

        public class ParamAsc : Param<ParamAsc>
        {
            public int Asc { get; set; }
            public override void SetValue(int value) { Asc = value; }
            public override ParamAsc GetObject() { return this; }
        }

        public class ParamExists : Param<ParamExists>
        {
            public int Exists { get; set; }
            public override void SetValue(int value) { Exists = value; }
            public override ParamExists GetObject() { return this; }
        }

        public class ParamLong : Param<ParamLong>
        {
            public int Long { get; set; }
            public override void SetValue(int value) { Long = value; }
            public override ParamLong GetObject() { return this; }
        }

        public class ParamPublic : Param<ParamPublic>
        {
            public int Public { get; set; }
            public override void SetValue(int value) { Public = value; }
            public override ParamPublic GetObject() { return this; }
        }

        public class ParamTrigger : Param<ParamTrigger>
        {
            public int Trigger { get; set; }
            public override void SetValue(int value) { Trigger = value; }
            public override ParamTrigger GetObject() { return this; }
        }

        public class ParamAudit : Param<ParamAudit>
        {
            public int Audit { get; set; }
            public override void SetValue(int value) { Audit = value; }
            public override ParamAudit GetObject() { return this; }
        }

        public class ParamFile : Param<ParamFile>
        {
            public int File { get; set; }
            public override void SetValue(int value) { File = value; }
            public override ParamFile GetObject() { return this; }
        }

        public class ParamMaxExtents : Param<ParamMaxExtents>
        {
            public int MaxExtents { get; set; }
            public override void SetValue(int value) { MaxExtents = value; }
            public override ParamMaxExtents GetObject() { return this; }
        }

        public class ParamRaw : Param<ParamRaw>
        {
            public int Raw { get; set; }
            public override void SetValue(int value) { Raw = value; }
            public override ParamRaw GetObject() { return this; }
        }

        public class ParamUid : Param<ParamUid>
        {
            public int Uid { get; set; }
            public override void SetValue(int value) { Uid = value; }
            public override ParamUid GetObject() { return this; }
        }

        public class ParamBetween : Param<ParamBetween>
        {
            public int Between { get; set; }
            public override void SetValue(int value) { Between = value; }
            public override ParamBetween GetObject() { return this; }
        }

        public class ParamMinus : Param<ParamMinus>
        {
            public int Minus { get; set; }
            public override void SetValue(int value) { Minus = value; }
            public override ParamMinus GetObject() { return this; }
        }

        public class ParamRename : Param<ParamRename>
        {
            public int Rename { get; set; }
            public override void SetValue(int value) { Rename = value; }
            public override ParamRename GetObject() { return this; }
        }

        public class ParamUnion : Param<ParamUnion>
        {
            public int Union { get; set; }
            public override void SetValue(int value) { Union = value; }
            public override ParamUnion GetObject() { return this; }
        }

        public class ParamBy : Param<ParamBy>
        {
            public int By { get; set; }
            public override void SetValue(int value) { By = value; }
            public override ParamBy GetObject() { return this; }
        }

        public class ParamFor : Param<ParamFor>
        {
            public int For { get; set; }
            public override void SetValue(int value) { For = value; }
            public override ParamFor GetObject() { return this; }
        }

        public class ParamMlsLabel : Param<ParamMlsLabel>
        {
            public int MlsLabel { get; set; }
            public override void SetValue(int value) { MlsLabel = value; }
            public override ParamMlsLabel GetObject() { return this; }
        }

        public class ParamResource : Param<ParamResource>
        {
            public int Resource { get; set; }
            public override void SetValue(int value) { Resource = value; }
            public override ParamResource GetObject() { return this; }
        }

        public class ParamUnique : Param<ParamUnique>
        {
            public int Unique { get; set; }
            public override void SetValue(int value) { Unique = value; }
            public override ParamUnique GetObject() { return this; }
        }

        public class ParamChar : Param<ParamChar>
        {
            public int Char { get; set; }
            public override void SetValue(int value) { Char = value; }
            public override ParamChar GetObject() { return this; }
        }

        public class ParamFrom : Param<ParamFrom>
        {
            public int From { get; set; }
            public override void SetValue(int value) { From = value; }
            public override ParamFrom GetObject() { return this; }
        }

        public class ParamMode : Param<ParamMode>
        {
            public int Mode { get; set; }
            public override void SetValue(int value) { Mode = value; }
            public override ParamMode GetObject() { return this; }
        }

        public class ParamRevoke : Param<ParamRevoke>
        {
            public int Revoke { get; set; }
            public override void SetValue(int value) { Revoke = value; }
            public override ParamRevoke GetObject() { return this; }
        }

        public class ParamUpdate : Param<ParamUpdate>
        {
            public int Update { get; set; }
            public override void SetValue(int value) { Update = value; }
            public override ParamUpdate GetObject() { return this; }
        }

        public class ParamCheck : Param<ParamCheck>
        {
            public int Check { get; set; }
            public override void SetValue(int value) { Check = value; }
            public override ParamCheck GetObject() { return this; }
        }

        public class ParamGrant : Param<ParamGrant>
        {
            public int Grant { get; set; }
            public override void SetValue(int value) { Grant = value; }
            public override ParamGrant GetObject() { return this; }
        }

        public class ParamModify : Param<ParamModify>
        {
            public int Modify { get; set; }
            public override void SetValue(int value) { Modify = value; }
            public override ParamModify GetObject() { return this; }
        }

        public class ParamRow : Param<ParamRow>
        {
            public int Row { get; set; }
            public override void SetValue(int value) { Row = value; }
            public override ParamRow GetObject() { return this; }
        }

        public class ParamCluster : Param<ParamCluster>
        {
            public int Cluster { get; set; }
            public override void SetValue(int value) { Cluster = value; }
            public override ParamCluster GetObject() { return this; }
        }

        public class ParamGroup : Param<ParamGroup>
        {
            public int Group { get; set; }
            public override void SetValue(int value) { Group = value; }
            public override ParamGroup GetObject() { return this; }
        }

        public class ParamNoAudit : Param<ParamNoAudit>
        {
            public int NoAudit { get; set; }
            public override void SetValue(int value) { NoAudit = value; }
            public override ParamNoAudit GetObject() { return this; }
        }

        public class ParamRowId : Param<ParamRowId>
        {
            public int RowId { get; set; }
            public override void SetValue(int value) { RowId = value; }
            public override ParamRowId GetObject() { return this; }
        }

        public class ParamValidate : Param<ParamValidate>
        {
            public int Validate { get; set; }
            public override void SetValue(int value) { Validate = value; }
            public override ParamValidate GetObject() { return this; }
        }

        public class ParamColumn : Param<ParamColumn>
        {
            public int Column { get; set; }
            public override void SetValue(int value) { Column = value; }
            public override ParamColumn GetObject() { return this; }
        }

        public class ParamHaving : Param<ParamHaving>
        {
            public int Having { get; set; }
            public override void SetValue(int value) { Having = value; }
            public override ParamHaving GetObject() { return this; }
        }

        public class ParamNoCompress : Param<ParamNoCompress>
        {
            public int NoCompress { get; set; }
            public override void SetValue(int value) { NoCompress = value; }
            public override ParamNoCompress GetObject() { return this; }
        }

        public class ParamRowNum : Param<ParamRowNum>
        {
            public int RowNum { get; set; }
            public override void SetValue(int value) { RowNum = value; }
            public override ParamRowNum GetObject() { return this; }
        }

        public class ParamValues : Param<ParamValues>
        {
            public int Values { get; set; }
            public override void SetValue(int value) { Values = value; }
            public override ParamValues GetObject() { return this; }
        }

        public class ParamIdentified : Param<ParamIdentified>
        {
            public int Identified { get; set; }
            public override void SetValue(int value) { Identified = value; }
            public override ParamIdentified GetObject() { return this; }
        }

        public class ParamNot : Param<ParamNot>
        {
            public int Not { get; set; }
            public override void SetValue(int value) { Not = value; }
            public override ParamNot GetObject() { return this; }
        }

        public class ParamRows : Param<ParamRows>
        {
            public int Rows { get; set; }
            public override void SetValue(int value) { Rows = value; }
            public override ParamRows GetObject() { return this; }
        }

        public class ParamVarchar : Param<ParamVarchar>
        {
            public int Varchar { get; set; }
            public override void SetValue(int value) { Varchar = value; }
            public override ParamVarchar GetObject() { return this; }
        }

        public class ParamCompress : Param<ParamCompress>
        {
            public int Compress { get; set; }
            public override void SetValue(int value) { Compress = value; }
            public override ParamCompress GetObject() { return this; }
        }

        public class ParamImmediate : Param<ParamImmediate>
        {
            public int Immediate { get; set; }
            public override void SetValue(int value) { Immediate = value; }
            public override ParamImmediate GetObject() { return this; }
        }

        public class ParamNoWait : Param<ParamNoWait>
        {
            public int NoWait { get; set; }
            public override void SetValue(int value) { NoWait = value; }
            public override ParamNoWait GetObject() { return this; }
        }

        public class ParamSelect : Param<ParamSelect>
        {
            public int Select { get; set; }
            public override void SetValue(int value) { Select = value; }
            public override ParamSelect GetObject() { return this; }
        }

        public class ParamVarchar2 : Param<ParamVarchar2>
        {
            public int Varchar2 { get; set; }
            public override void SetValue(int value) { Varchar2 = value; }
            public override ParamVarchar2 GetObject() { return this; }
        }

        public class ParamConnect : Param<ParamConnect>
        {
            public int Connect { get; set; }
            public override void SetValue(int value) { Connect = value; }
            public override ParamConnect GetObject() { return this; }
        }

        public class ParamIn : Param<ParamIn>
        {
            public int In { get; set; }
            public override void SetValue(int value) { In = value; }
            public override ParamIn GetObject() { return this; }
        }

        public class ParamNull : Param<ParamNull>
        {
            public int Null { get; set; }
            public override void SetValue(int value) { Null = value; }
            public override ParamNull GetObject() { return this; }
        }

        public class ParamSession : Param<ParamSession>
        {
            public int Session { get; set; }
            public override void SetValue(int value) { Session = value; }
            public override ParamSession GetObject() { return this; }
        }

        public class ParamView : Param<ParamView>
        {
            public int View { get; set; }
            public override void SetValue(int value) { View = value; }
            public override ParamView GetObject() { return this; }
        }

        public class ParamCreate : Param<ParamCreate>
        {
            public int Create { get; set; }
            public override void SetValue(int value) { Create = value; }
            public override ParamCreate GetObject() { return this; }
        }

        public class ParamIncrement : Param<ParamIncrement>
        {
            public int Increment { get; set; }
            public override void SetValue(int value) { Increment = value; }
            public override ParamIncrement GetObject() { return this; }
        }

        public class ParamNumber : Param<ParamNumber>
        {
            public int Number { get; set; }
            public override void SetValue(int value) { Number = value; }
            public override ParamNumber GetObject() { return this; }
        }

        public class ParamSet : Param<ParamSet>
        {
            public int Set { get; set; }
            public override void SetValue(int value) { Set = value; }
            public override ParamSet GetObject() { return this; }
        }

        public class ParamWhenever : Param<ParamWhenever>
        {
            public int Whenever { get; set; }
            public override void SetValue(int value) { Whenever = value; }
            public override ParamWhenever GetObject() { return this; }
        }

        public class ParamCurrent : Param<ParamCurrent>
        {
            public int Current { get; set; }
            public override void SetValue(int value) { Current = value; }
            public override ParamCurrent GetObject() { return this; }
        }

        public class ParamOf : Param<ParamOf>
        {
            public int Of { get; set; }
            public override void SetValue(int value) { Of = value; }
            public override ParamOf GetObject() { return this; }
        }

        public class ParamShare : Param<ParamShare>
        {
            public int Share { get; set; }
            public override void SetValue(int value) { Share = value; }
            public override ParamShare GetObject() { return this; }
        }

        public class ParamWhere : Param<ParamWhere>
        {
            public int Where { get; set; }
            public override void SetValue(int value) { Where = value; }
            public override ParamWhere GetObject() { return this; }
        }

        public class ParamInitial : Param<ParamInitial>
        {
            public int Initial { get; set; }
            public override void SetValue(int value) { Initial = value; }
            public override ParamInitial GetObject() { return this; }
        }

        public class ParamOffline : Param<ParamOffline>
        {
            public int Offline { get; set; }
            public override void SetValue(int value) { Offline = value; }
            public override ParamOffline GetObject() { return this; }
        }

        public class ParamSize : Param<ParamSize>
        {
            public int Size { get; set; }
            public override void SetValue(int value) { Size = value; }
            public override ParamSize GetObject() { return this; }
        }

        public class ParamWith : Param<ParamWith>
        {
            public int With { get; set; }
            public override void SetValue(int value) { With = value; }
            public override ParamWith GetObject() { return this; }
        }

        public class ParamInsert : Param<ParamInsert>
        {
            public int Insert { get; set; }
            public override void SetValue(int value) { Insert = value; }
            public override ParamInsert GetObject() { return this; }
        }

        public class ParamOn : Param<ParamOn>
        {
            public int On { get; set; }
            public override void SetValue(int value) { On = value; }
            public override ParamOn GetObject() { return this; }
        }

        public class ParamSmallint : Param<ParamSmallint>
        {
            public int Smallint { get; set; }
            public override void SetValue(int value) { Smallint = value; }
            public override ParamSmallint GetObject() { return this; }
        }

        public class ParamTestBo
        {
            public int Id { get; set; }
            public string Info { get; set; }
            public int Int { get; set; }
            public double Double { get; set; }
            public bool? NullableBool { get; set; }
            public DateTime? DateTime { get; set; }
            public uint? UInt { get; set; }
        }

        public class ParamRelBo
        {
            [Sequence("SEQ_PARAMTESTREL_ID")]
            [PrimaryKey]
            [Alias("ParamRel_Id")]
            public int Id { get; set; }
            [ForeignKey(typeof(ParamTestBo))]
            public int PtId { get; set; }

            [Alias("InfoStr")]
            public string Info { get; set; }
        }

        public class ParamByteBo
        {
            public int Id { get; set; }
            public byte[] Data { get; set; }
        }

        public class ParamLevelAsAlias
        {
            public int Id { get; set; }

            [Alias("Level")]
            public int ApprovalLevel { get; set; }
        }
    }
}
