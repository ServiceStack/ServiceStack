using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using ServiceStack.Script;
using ServiceStack.Text;

namespace ServiceStack.WebHost.Endpoints.Tests.ScriptTests
{
    public class ScriptLispLinqTests
    {
        private static ScriptContext CreateContext()
        {
            var context = new ScriptContext
            {
                ScriptLanguages = { ScriptLisp.Language },
                AllowScriptingOfAllTypes = true,
                ScriptNamespaces = {
                    "System"
                },
                ScriptMethods = {
                    new ProtectedScripts(),
                },
                Args =
                {
                    [ScriptConstants.DefaultDateFormat] = "yyyy/MM/dd",
                    ["products"] = QueryData.Products,
                    ["customers"] = QueryData.Customers,
                    ["comparer"] = new CaseInsensitiveComparer(),
                    ["anagramComparer"] = new QueryFilterTests.AnagramEqualityComparer(),
                }
            };
            Lisp.Set("products-list", Lisp.ToCons(QueryData.Products));
            Lisp.Set("customers-list", Lisp.ToCons(QueryData.Customers));
            return context.Init();
        }

        [SetUp]
        public void Setup() => context = CreateContext();
        private ScriptContext context;

        string render(string lisp) => context.RenderLisp(lisp).NormalizeNewLines();

        void print(string lisp)
        {
            "expr: ".Print();
            Lisp.Parse(lisp).Each(x => Lisp.Str(x).Print());
            "result: ".Print();
            eval(lisp).PrintDump();
        }

        object eval(string lisp) => context.EvaluateLisp($"(return (let () {lisp} ))");

        [Test]
        public void Linq01() 
        {
            Assert.That(render(@"
(defn linq01 ()
    (setq numbers '(5 4 1 3 9 8 6 7 2 0))
    (let ((low-numbers (filter #(< % 5) numbers)))
        (println ""Numbers < 5:"")
        (dolist (n low-numbers)
            (println n))))
(linq01)"), 
                
                Is.EqualTo(@"
Numbers < 5:
4
1
3
2
0
".NormalizeNewLines()));
        }

        [Test]
        public void Linq02()
        {
            Assert.That(render(@"
(defn linq02 ()
    (let ( (sold-out-products 
               (filter #(= 0 (.UnitsInStock %)) products-list)) )
        (println ""Sold out products:"")
        (doseq (p sold-out-products)
            (println (.ProductName p) "" is sold out"") )
    ))
(linq02)"), 
                
                Is.EqualTo(@"
Sold out products:
Chef Anton's Gumbo Mix is sold out
Alice Mutton is sold out
Thüringer Rostbratwurst is sold out
Gorgonzola Telino is sold out
Perth Pasties is sold out
".NormalizeNewLines()));
        }

        [Test]
        public void Linq03()
        {
            Assert.That(render(@"
(defn linq03 ()
  (let ( (expensive-in-stock-products
            (filter #(and
                     (> (.UnitsInStock %) 0)
                     (> (.UnitPrice %) 3))
             products-list)
         ))
    (println ""In-stock products that cost more than 3.00:"")
    (doseq (p expensive-in-stock-products)
      (println (.ProductName p) "" is in stock and costs more than 3.00""))))

(linq03)"), 
                
                Does.StartWith(@"
In-stock products that cost more than 3.00:
Chai is in stock and costs more than 3.00
Chang is in stock and costs more than 3.00
Aniseed Syrup is in stock and costs more than 3.00
Chef Anton's Cajun Seasoning is in stock and costs more than 3.00
Grandma's Boysenberry Spread is in stock and costs more than 3.00
".NormalizeNewLines()));
        }

        [Test]
        public void Linq04()
        {
            Assert.That(render(@"
(defn linq04 ()
    (let ( (wa-customers (filter #(= (.Region %) ""WA"") customers-list)) )
        (println ""Customers from Washington and their orders:"")
        (doseq (c wa-customers)
            (println ""Customer "" (.CustomerId c) "": "" (.CompanyName c) "": "")
            (doseq (o (.Orders c))
                (println ""    Order "" (.OrderId o) "": "" (.OrderDate o)) )
        )))
(linq04)"), 
                
                Does.StartWith(@"
Customers from Washington and their orders:
Customer LAZYK: Lazy K Kountry Store: 
    Order 10482: 3/21/1997 12:00:00 AM
    Order 10545: 5/22/1997 12:00:00 AM
Customer TRAIH: Trail's Head Gourmet Provisioners: 
    Order 10574: 6/19/1997 12:00:00 AM
    Order 10577: 6/23/1997 12:00:00 AM
    Order 10822: 1/8/1998 12:00:00 AM
".NormalizeNewLines()));
        }

        [Test]
        public void Linq05()
        {
            Assert.That(render(@"
(defn linq05 ()
    (let ( (digits '(""zero"" ""one"" ""two"" ""three"" ""four"" ""five"" ""six"" ""seven"" ""eight"" ""nine""))
           (short-digits) )
        (setq short-digits (filter-index (fn (x i) (> i (length x))) digits) )
        (println ""Short digits:"")
        (doseq (d short-digits)
          (println ""The word "" d "" is shorter than its value""))
    ))
(linq05)"), 
                
                Does.StartWith(@"
Short digits:
The word five is shorter than its value
The word six is shorter than its value
The word seven is shorter than its value
The word eight is shorter than its value
The word nine is shorter than its value
".NormalizeNewLines()));
        }

        [Test]
        public void Linq06()
        {
            Assert.That(render(@"
(defn linq06 ()
  (let ( (numbers '(5 4 1 3 9 8 6 7 2 0)) (nums-plus-one) )
    (setq nums-plus-one (map inc numbers))
    (println ""Numbers + 1:"")
        (doseq (n nums-plus-one) (println n))))
(linq06)"), 
                
                Does.StartWith(@"
Numbers + 1:
6
5
2
4
10
9
7
8
3
1
".NormalizeNewLines()));
        }

        [Test]
        public void Linq07()
        {
            Assert.That(render(@"
(defn linq07 ()
  (let ( (product-names (map #(.ProductName %) products-list)) )
    (println ""Product Names:"")
    (doseq (x product-names) (println x))))
(linq07)"), 
                
                Does.StartWith(@"
Product Names:
Chai
Chang
Aniseed Syrup
Chef Anton's Cajun Seasoning
Chef Anton's Gumbo Mix
".NormalizeNewLines()));
        }

        [Test]
        public void Linq08()
        {
            Assert.That(render(@"
(defn linq08 ()
  (let ( (numbers '(5 4 1 3 9 8 6 7 2 0))
         (strings '(""zero"" ""one"" ""two"" ""three"" ""four"" ""five"" ""six"" ""seven"" ""eight"" ""nine"")) 
         (text-nums) )
      (setq text-nums (map #(nth strings %) numbers))
      (println ""Number strings:"")
      (doseq (n text-nums) (println n))
  ))
(linq08)"), 
                
                Does.StartWith(@"
Number strings:
five
four
one
three
nine
eight
six
seven
two
zero
".NormalizeNewLines()));
        }

        [Test]
        public void Linq09()
        {
            Assert.That(render(@"
(defn linq09 ()
  (let ( (words '(""aPPLE"" ""BlUeBeRrY"" ""cHeRry""))
         (upper-lower-words) )
    (setq upper-lower-words
        (map (fn (w) { :lower (lower-case w) :upper (upper-case w) } ) words) )
    (doseq (ul upper-lower-words)
        (println ""Uppercase: "" (:upper ul) "", Lowercase: "" (:lower ul)))
  ))
(linq09)"), 
                
                Does.StartWith(@"
Uppercase: APPLE, Lowercase: apple
Uppercase: BLUEBERRY, Lowercase: blueberry
Uppercase: CHERRY, Lowercase: cherry
".NormalizeNewLines()));
        }

        [Test]
        public void Linq09_classic_lisp()
        {
            Assert.That(render(@"
(defn linq09 ()
  (let ( (words '(""aPPLE"" ""BlUeBeRrY"" ""cHeRry""))
         (upper-lower-words) )
    (setq upper-lower-words
        (map (fn (w) `( (lower ,(lower-case w)) (upper ,(upper-case w)) )) words) )
    (doseq (ul upper-lower-words)
        (println ""Uppercase: "" (assoc-value 'upper ul) "", Lowercase: "" (assoc-value 'lower ul)))
  ))
(linq09)"), 
                
                Does.StartWith(@"
Uppercase: APPLE, Lowercase: apple
Uppercase: BLUEBERRY, Lowercase: blueberry
Uppercase: CHERRY, Lowercase: cherry
".NormalizeNewLines()));
        }

        [Test]
        public void Linq10()
        {
            Assert.That(render(@"
(defn linq10 ()
  (let ( (numbers '(5 4 1 3 9 8 6 7 2 0))
         (strings '(""zero"" ""one"" ""two"" ""three"" ""four"" ""five"" ""six"" ""seven"" ""eight"" ""nine""))
         (digit-odd-evens) )
      (setq digit-odd-evens 
          (map (fn(n) { :digit (nth strings n) :even (even? n) } ) numbers))
      (doseq (d digit-odd-evens)
          (println ""The digit "" (:digit d) "" is "" (if (:even d) ""even"" ""odd"")))
  ))
(linq10)"), 
                
                Does.StartWith(@"
The digit five is odd
The digit four is even
The digit one is odd
The digit three is odd
The digit nine is odd
The digit eight is even
The digit six is even
The digit seven is odd
The digit two is even
The digit zero is even
".NormalizeNewLines()));
        }

        [Test]
        public void Linq11()
        {
            Assert.That(render(@"
(defn linq11 ()
  (let ( (product-infos
            (map (fn (x) {
                    :ProductName (.ProductName x)
                    :Category    (.Category x)
                    :Price       (.UnitPrice x) 
                 }) 
            products-list)) )
    (println ""Product Info:"")
    (doseq (p product-infos)
        (println (:ProductName p) "" is in the category "" (:Category p) "" and costs "" (:Price p)) )
  ))
(linq11)"), 
                
                Does.StartWith(@"
Product Info:
Chai is in the category Beverages and costs 18
Chang is in the category Beverages and costs 19
Aniseed Syrup is in the category Condiments and costs 10
Chef Anton's Cajun Seasoning is in the category Condiments and costs 22
Chef Anton's Gumbo Mix is in the category Condiments and costs 21.35
".NormalizeNewLines()));
        }

        [Test]
        public void Linq11_expanded_form()
        {
            Assert.That(render(@"
(defn linq11 ()
  (let ( (product-infos
            (map (fn (x) (new-map
                    (list ""ProductName"" (.ProductName x))
                    (list ""Category""    (.Category x))
                    (list ""Price""       (.UnitPrice x)) 
                )) 
            products-list)) )
    (println ""Product Info:"")
    (doseq (p product-infos)
      (println (:ProductName p) "" is in the category "" (:Category p) "" and costs "" (:Price p)))
  ))
(linq11)"), 
                
                Does.StartWith(@"
Product Info:
Chai is in the category Beverages and costs 18
Chang is in the category Beverages and costs 19
Aniseed Syrup is in the category Condiments and costs 10
Chef Anton's Cajun Seasoning is in the category Condiments and costs 22
Chef Anton's Gumbo Mix is in the category Condiments and costs 21.35
".NormalizeNewLines()));
        }

        [Test]
        public void Linq11_classic_lisp()
        {
            Assert.That(render(@"
(defn linq11 ()
  (let ( (product-infos
            (map (fn (p) `(
                    (ProductName ,(.ProductName p))
                    (Category    ,(.Category p))
                    (Price       ,(.UnitPrice p)) 
                )) 
            products-list)) )
    (println ""Product Info:"")
    (doseq (p product-infos)
      (println (assoc-value 'ProductName p) "" is in the category "" (assoc-value 'Category p) 
               "" and costs "" (assoc-value 'Price p)))
  ))
(linq11)"), 
                
                Does.StartWith(@"
Product Info:
Chai is in the category Beverages and costs 18
Chang is in the category Beverages and costs 19
Aniseed Syrup is in the category Condiments and costs 10
Chef Anton's Cajun Seasoning is in the category Condiments and costs 22
Chef Anton's Gumbo Mix is in the category Condiments and costs 21.35
".NormalizeNewLines()));
        }

        [Test]
        public void Linq12()
        {
            Assert.That(render(@"
(defn linq12 ()
  (let ( (numbers '(5 4 1 3 9 8 6 7 2 0))
         (i 0) (nums-in-place) )
    (setq nums-in-place (map (fn (n) { :num n :in-place (= n (1- (incf i))) }) numbers))
    (println ""Number: In-place?"")
    (doseq (n nums-in-place)
        (println (:num n) "": "" (if (:in-place n) 'true 'false)) )
  ))
(linq12)"), 
                
                Does.StartWith(@"
Number: In-place?
5: false
4: false
1: false
3: true
9: false
8: false
6: true
7: true
2: false
0: false
".NormalizeNewLines()));
        }

        [Test]
        public void Linq13()
        {
            Assert.That(render(@"
(defn linq13 ()
    (let ( (numbers '(5 4 1 3 9 8 6 7 2 0))
           (digits  '(""zero"" ""one"" ""two"" ""three"" ""four"" ""five"" ""six"" ""seven"" ""eight"" ""nine"")) 
           (low-nums) )
      (setq low-nums (map #(nth digits %) (filter #(< % 5) numbers)))
      (println ""Numbers < 5:"")
      (doseq (n low-nums) (println n))
    ))
(linq13)"), 
                
                Does.StartWith(@"
Numbers < 5:
four
one
three
two
zero
".NormalizeNewLines()));
        }

        [Test]
        public void Linq14()
        {
            Assert.That(render(@"
(defn linq14 ()
  (let ( (numbers-a '(0 2 4 5 6 8 9))
         (numbers-b '(1 3 5 7 8)) 
         (pairs) )    
    (setq pairs (filter #(< (:a %) (:b %)) 
                    (zip (fn (a b) { :a a, :b b }) numbers-a numbers-b)))        
    (println ""Pairs where a < b:"")
    (doseq (pair pairs)
      (println (:a pair) "" is less than "" (:b pair)))
  ))
(linq14)"), 
                
                Does.StartWith(@"
Pairs where a < b:
0 is less than 1
0 is less than 3
0 is less than 5
0 is less than 7
0 is less than 8
2 is less than 3
2 is less than 5
2 is less than 7
2 is less than 8
4 is less than 5
4 is less than 7
4 is less than 8
5 is less than 7
5 is less than 8
6 is less than 7
6 is less than 8
".NormalizeNewLines()));
        }

        [Test]
        public void Linq15()
        {
            Assert.That(render(@"
(defn linq15 ()
  (let ( (orders 
            (flatmap (fn (c)
              (map (fn (o) {
                :customer-id (.CustomerId c) 
                :order-id    (.OrderId o) 
                :total       (.Total o)
              }) (.Orders c))
             ) customers-list)) )
    (doseq (o orders) (dump-inline o))
  ))
(linq15)"), 
                
                Does.StartWith(@"
{customer-id:ALFKI,order-id:10643,total:814.5}
{customer-id:ALFKI,order-id:10692,total:878}
{customer-id:ALFKI,order-id:10702,total:330}
{customer-id:ALFKI,order-id:10835,total:845.8}
{customer-id:ALFKI,order-id:10952,total:471.2}
{customer-id:ALFKI,order-id:11011,total:933.5}
{customer-id:ANATR,order-id:10308,total:88.8}
{customer-id:ANATR,order-id:10625,total:479.75}
{customer-id:ANATR,order-id:10759,total:320}
{customer-id:ANATR,order-id:10926,total:514.4}
{customer-id:ANTON,order-id:10365,total:403.2}
".NormalizeNewLines()));
        }

        [Test]
        public void Linq16()
        {
            Assert.That(render(@"
(defn linq16 ()
  (let ( 
    (orders
        (flatmap (fn (c) 
            (flatmap (fn (o) 
                (if (> (.OrderDate o) (DateTime. 1998 1 1) )
                {
                    :customer-id (.CustomerId c) 
                    :order-id    (.OrderId o) 
                    :order-date  (.OrderDate o)
                })) (.Orders c) )
        ) customers-list) 
    ))
    (doseq (o orders) (dump-inline o))
  ))
(linq16)"), 
                
                Does.StartWith(@"
{customer-id:ALFKI,order-id:10952,order-date:1998-03-16}
{customer-id:ALFKI,order-id:11011,order-date:1998-04-09}
{customer-id:ANATR,order-id:10926,order-date:1998-03-04}
{customer-id:ANTON,order-id:10856,order-date:1998-01-28}
".NormalizeNewLines()));
        }

        [Test]
        public void test()
        {
//            print(@"");

//            print("(setq numbers-a '(1 2 3)) (setq numbers-b '(3 4 5)) (zip (fn (a b) { :a a :b b }) numbers-a numbers-b)");
//            print("(map #(* 2 %) (range 10))");
//            print("(fn (x) (.ProductName x))");
//            print(@"(fn (x) (new-map (list ""ProductName"" (.ProductName x)) ))");
        }
        
    }
}
