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
                    "System",
                    typeof(CaseInsensitiveComparer).Namespace, //System.Collections
                    typeof(AnagramEqualityComparer).Namespace,
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
                    ["anagramComparer"] = new AnagramEqualityComparer(),
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
(defn linq01 []
    (setq numbers [5 4 1 3 9 8 6 7 2 0])
    (let ((low-numbers (where #(< % 5) numbers)))
        (println ""Numbers < 5:"")
        (doseq (n low-numbers)
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
(defn linq02 []
    (let ( (sold-out-products 
               (where #(= 0 (.UnitsInStock %)) products-list)) )
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
(defn linq03 []
  (let ( (expensive-in-stock-products
            (where #(and
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
        [Ignore("Needs review - MONOREPO")]
        public void Linq04()
        {
            Assert.That(render(@"
(defn linq04 []
    (let ( (wa-customers (where #(= (.Region %) ""WA"") customers-list)) )
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
(defn linq05 []
    (let ( (digits [""zero"" ""one"" ""two"" ""three"" ""four"" ""five"" ""six"" ""seven"" ""eight"" ""nine""])
           (short-digits) )
        (setq short-digits (where-index (fn [x i] (> i (length x))) digits) )
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
(defn linq06 []
  (let ( (numbers [5 4 1 3 9 8 6 7 2 0]) (nums-plus-one) )
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
(defn linq07 []
  (let ( (product-names (map .ProductName products-list)) )
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
(defn linq08 []
  (let ( (numbers [5 4 1 3 9 8 6 7 2 0])
         (strings [""zero"" ""one"" ""two"" ""three"" ""four"" ""five"" ""six"" ""seven"" ""eight"" ""nine""]) 
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
(defn linq09 []
  (let ( (words [""aPPLE"" ""BlUeBeRrY"" ""cHeRry""])
         (upper-lower-words) )
    (setq upper-lower-words
        (map (fn [w] { :lower (lower-case w) :upper (upper-case w) } ) words) )
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
        public void Linq10()
        {
            Assert.That(render(@"
(defn linq10 []
  (let ( (numbers [5 4 1 3 9 8 6 7 2 0])
         (strings [""zero"" ""one"" ""two"" ""three"" ""four"" ""five"" ""six"" ""seven"" ""eight"" ""nine""])
         (digit-odd-evens) )
      (setq digit-odd-evens 
          (map (fn [n] { :digit (nth strings n) :even (even? n) } ) numbers))
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
        public void Linq09_classic_lisp()
        {
            Assert.That(render(@"
(defn linq09 []
  (let ( (words [""aPPLE"" ""BlUeBeRrY"" ""cHeRry""])
         (upper-lower-words) )
    (setq upper-lower-words
        (map (fn [w] `( (lower ,(lower-case w)) (upper ,(upper-case w)) )) words) )
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
        public void Linq11()
        {
            Assert.That(render(@"
(defn linq11 []
  (let ( (product-infos
            (map (fn [x] {
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
(defn linq11 []
  (let ( (product-infos
            (map (fn [x] (new-map
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
(defn linq11 []
  (let ( (product-infos
            (map (fn [p] `(
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
(defn linq12 []
  (let ( (numbers [5 4 1 3 9 8 6 7 2 0])
         (i 0) (nums-in-place) )
    (setq nums-in-place (map (fn [n] { :num n :in-place (= n (f++ i)) }) numbers))
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
(defn linq13 []
    (let ( (numbers [5 4 1 3 9 8 6 7 2 0])
           (digits  [""zero"" ""one"" ""two"" ""three"" ""four"" ""five"" ""six"" ""seven"" ""eight"" ""nine""]) )
        (println ""Numbers < 5:"")
        (joinln (map #(nth digits %) (where #(< % 5) numbers)))
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
(defn linq14 []
  (let ( (numbers-a [0 2 4 5 6 8 9])
         (numbers-b [1 3 5 7 8]) 
         (pairs) )    
    (setq pairs (where #(< (:a %) (:b %)) 
                    (zip (fn [a b] { :a a, :b b }) numbers-a numbers-b)))        
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
        public void Linq14_zip_where()
        {
            Assert.That(render(@"
(defn linq14 []
  (let ( (numbers-a [0 2 4 5 6 8 9])
         (numbers-b [1 3 5 7 8]) 
         (pairs) )    
    (setq pairs 
        (zip-where #(< %1 %2) #(it { :a %1, :b %2 }) numbers-a numbers-b))        
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
        public void Linq14_doseq()
        {
            Assert.That(render(@"
(defn linq14 []
  (let ( (numbers-a [0 2 4 5 6 8 9])
       (numbers-b [1 3 5 7 8]) 
       (pairs) )
    (doseq (a numbers-a)
        (doseq (b numbers-b)
            (if (< a b) (push { :a a, :b b } pairs))))
    (println ""Pairs where a < b:"")
    (doseq (pair (nreverse pairs))
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
(defn linq15 []
  (let ( (orders (flatmap (fn [c]
                    (map (fn [o] {
                        :customer-id (.CustomerId c) 
                        :order-id    (.OrderId o) 
                        :total       (.Total o)
                    }) (where #(< (.Total %) 500) (.Orders c)) ))
                customers-list)) )
    (doseq (o orders) (dump-inline o))
  ))
(linq15)"), 
                
                Does.StartWith(@"
{customer-id:ALFKI,order-id:10702,total:330}
{customer-id:ALFKI,order-id:10952,total:471.2}
{customer-id:ANATR,order-id:10308,total:88.8}
{customer-id:ANATR,order-id:10625,total:479.75}
{customer-id:ANATR,order-id:10759,total:320}
{customer-id:ANTON,order-id:10365,total:403.2}
{customer-id:ANTON,order-id:10682,total:375.5}
{customer-id:AROUT,order-id:10355,total:480}
{customer-id:AROUT,order-id:10453,total:407.7}
{customer-id:AROUT,order-id:10741,total:228}
".NormalizeNewLines()));
        }

        [Test]
        public void Linq16()
        {
            Assert.That(render(@"
(defn linq16 []
  (let ( 
        (orders (flatmap (fn [c] 
                    (map-where #(> (.OrderDate %) (DateTime. 1998 1 1)) 
                        #(it {
                            :customer-id (.CustomerId c) 
                            :order-id    (.OrderId %) 
                            :order-date  (.OrderDate %)
                        }) (.Orders c) )
                ) customers-list) ))
    (doseq (o orders) (dump-inline o))
  ))
(linq16)"), 
                
                Does.StartWith(@"
{customer-id:ALFKI,order-id:10835,order-date:1998-01-15}
{customer-id:ALFKI,order-id:10952,order-date:1998-03-16}
{customer-id:ALFKI,order-id:11011,order-date:1998-04-09}
{customer-id:ANATR,order-id:10926,order-date:1998-03-04}
{customer-id:ANTON,order-id:10856,order-date:1998-01-28}
".NormalizeNewLines()));
        }

        [Test]
        public void Linq17()
        {
            Assert.That(render(@"
(defn linq17 []
  (let ( 
        (orders (flatmap (fn [c] 
                    (map-where #(>= (:total %) 2000)
                        #(it {
                            :customer-id (.CustomerId c) 
                            :order-id    (.OrderId %) 
                            :total       (.Total %)
                        }) (.Orders c) )
                ) customers-list) ))
    (doseq (o orders) (dump-inline o))
  ))
(linq17)"), 
                
                Does.StartWith(@"
{customer-id:ANTON,order-id:10573,total:2082}
{customer-id:AROUT,order-id:10558,total:2142.9}
{customer-id:AROUT,order-id:10953,total:4441.25}
{customer-id:BERGS,order-id:10384,total:2222.4}
{customer-id:BERGS,order-id:10524,total:3192.65}
".NormalizeNewLines()));
        }

        [Test]
        public void Linq18()
        {
            Assert.That(render(@"
(defn linq18 []
  (let ( (cutoff-date (DateTime. 1997 1 1))
         (orders) )
    (setq orders (flatmap (fn [c] 
          (map-where #(>= (.OrderDate %) cutoff-date) 
              #(it {
                  :customer-id (.CustomerId c) 
                  :order-id    (.OrderId %) 
              }) (.Orders c) )
      ) (where #(= (.Region %) ""WA"") customers-list) ) )
    (doseq (o orders) (dump-inline o))
  ))
(linq18)"), 
                
                Does.StartWith(@"
{customer-id:LAZYK,order-id:10482}
{customer-id:LAZYK,order-id:10545}
{customer-id:TRAIH,order-id:10574}
{customer-id:TRAIH,order-id:10577}
{customer-id:TRAIH,order-id:10822}
{customer-id:WHITC,order-id:10469}
{customer-id:WHITC,order-id:10483}
{customer-id:WHITC,order-id:10504}
{customer-id:WHITC,order-id:10596}
{customer-id:WHITC,order-id:10693}
{customer-id:WHITC,order-id:10696}
{customer-id:WHITC,order-id:10723}
{customer-id:WHITC,order-id:10740}
{customer-id:WHITC,order-id:10861}
{customer-id:WHITC,order-id:10904}
{customer-id:WHITC,order-id:11032}
{customer-id:WHITC,order-id:11066}
".NormalizeNewLines()));
        }

        [Test]
        public void Linq19()
        {
            Assert.That(render(@"
(defn linq19 []
  (let ( (customer-orders 
            (map 
                #(str ""Customer #"" (:i %) "" has an order with OrderID "" (.OrderId (:o %))) 
                (flatten (map-index (fn (c i) (map #(it { :o % :i (1+ i) }) (.Orders c))) customers-list)) 
            )) )
    (doseq (x customer-orders) (println x))
  ))
(linq19)"), 
                
                Does.StartWith(@"
Customer #1 has an order with OrderID 10643
Customer #1 has an order with OrderID 10692
Customer #1 has an order with OrderID 10702
Customer #1 has an order with OrderID 10835
Customer #1 has an order with OrderID 10952
Customer #1 has an order with OrderID 11011
Customer #2 has an order with OrderID 10308
Customer #2 has an order with OrderID 10625
Customer #2 has an order with OrderID 10759
Customer #2 has an order with OrderID 10926
".NormalizeNewLines()));
        }

        [Test]
        public void Linq20()
        {
            Assert.That(render(@"
(defn linq20 []
  (let ( (numbers [5 4 1 3 9 8 6 7 2 0])
         (first-3-numbers) ) 
    (setq first-3-numbers (take 3 numbers))
    (println ""First 3 numbers:"")
    (doseq (n first-3-numbers) (println n))
  ))
(linq20)"), 
                
                Does.StartWith(@"
First 3 numbers:
5
4
1
".NormalizeNewLines()));
        }

        [Test]
        public void Linq21()
        {
            Assert.That(render(@"
(defn linq21 []
  (let ( (first-3-wa-orders) )
    (setq first-3-wa-orders 
      (take 3 
        (flatmap (fn [c] 
          (map #(it {
                  :customer-id (.CustomerId c) 
                  :order-id    (.OrderId %) 
                  :order-date  (.OrderDate %) }) 
            (.Orders c) )) 
        (where #(= (.Region %) ""WA"") customers-list) )) )
    (println ""First 3 orders in WA:"")
    (doseq (x first-3-wa-orders) (dump-inline x))
  ))
(linq21)"), 
                
                Does.StartWith(@"
First 3 orders in WA:
{customer-id:LAZYK,order-id:10482,order-date:1997-03-21}
{customer-id:LAZYK,order-id:10545,order-date:1997-05-22}
{customer-id:TRAIH,order-id:10574,order-date:1997-06-19}
".NormalizeNewLines()));
        }

        [Test]
        public void Linq22()
        {
            Assert.That(render(@"
(defn linq22 []
  (let ( (numbers [5 4 1 3 9 8 6 7 2 0]) 
         (all-but-first-4-numbers) )
        (setq all-but-first-4-numbers (skip 4 numbers))
    (println ""All but first 4 numbers:"")
    (doseq (n all-but-first-4-numbers) (println n))
  ))
(linq22)"), 
                
                Does.StartWith(@"
All but first 4 numbers:
9
8
6
7
2
0
".NormalizeNewLines()));
        }

        [Test]
        public void Linq23()
        {
            Assert.That(render(@"
(defn linq23 []
  (let ( (all-but-first-2-orders
      (skip 2 
        (flatmap (fn [c] 
          (map #(it {
              :customer-id (.CustomerId c) 
              :order-id    (.OrderId %) 
              :order-date  (.OrderDate %) }) 
            (.Orders c) )) 
          (where #(= (.Region %) ""WA"") customers-list) )) ))
    (println ""All but first 2 orders in WA:"")
    (doseq (o all-but-first-2-orders) (dump-inline o))
  ))
(linq23)"), 
                
                Does.StartWith(@"
All but first 2 orders in WA:
{customer-id:TRAIH,order-id:10574,order-date:1997-06-19}
{customer-id:TRAIH,order-id:10577,order-date:1997-06-23}
{customer-id:TRAIH,order-id:10822,order-date:1998-01-08}
{customer-id:WHITC,order-id:10269,order-date:1996-07-31}
{customer-id:WHITC,order-id:10344,order-date:1996-11-01}
{customer-id:WHITC,order-id:10469,order-date:1997-03-10}
{customer-id:WHITC,order-id:10483,order-date:1997-03-24}
{customer-id:WHITC,order-id:10504,order-date:1997-04-11}
{customer-id:WHITC,order-id:10596,order-date:1997-07-11}
{customer-id:WHITC,order-id:10693,order-date:1997-10-06}
{customer-id:WHITC,order-id:10696,order-date:1997-10-08}
{customer-id:WHITC,order-id:10723,order-date:1997-10-30}
{customer-id:WHITC,order-id:10740,order-date:1997-11-13}
{customer-id:WHITC,order-id:10861,order-date:1998-01-30}
{customer-id:WHITC,order-id:10904,order-date:1998-02-24}
{customer-id:WHITC,order-id:11032,order-date:1998-04-17}
{customer-id:WHITC,order-id:11066,order-date:1998-05-01}
".NormalizeNewLines()));
        }

        [Test]
        public void Linq24()
        {
            Assert.That(render(@"
(defn linq24 []
  (let ( (numbers [5 4 1 3 9 8 6 7 2 0])
         (first-numbers-less-than-6) )
    (setq first-numbers-less-than-6 (take-while #(< % 6) numbers))
    (println ""First numbers less than 6:"")
    (doseq (n first-numbers-less-than-6) (println n))
  ))
(linq24)"), 
                
                Does.StartWith(@"
First numbers less than 6:
5
4
1
3
".NormalizeNewLines()));
        }

        [Test]
        public void Linq25()
        {
            Assert.That(render(@"
(defn linq25 []
  (let ( (numbers [5 4 1 3 9 8 6 7 2 0] )
         (i 0) (first-small-numbers) )
    (setq first-small-numbers (take-while #(>= % (f++ i)) numbers) )
    (println ""First numbers not less than their position:"")
    (doseq (n first-small-numbers) (println n))
  ))
(linq25)"), 
                
                Does.StartWith(@"
First numbers not less than their position:
5
4
".NormalizeNewLines()));
        }

        [Test]
        public void Linq26()
        {
            Assert.That(render(@"
(defn linq26 []
  (let ( (numbers [5 4 1 3 9 8 6 7 2 0])
         (all-but-first-3-numbers) )
    (setq all-but-first-3-numbers (skip-while #(not= (mod % 3) 0) numbers))
    (println ""All elements starting from first element divisible by 3:"")
    (doseq (n all-but-first-3-numbers) (println n))
  ))
(linq26)"), 
                
                Does.StartWith(@"
All elements starting from first element divisible by 3:
3
9
8
6
7
2
0
".NormalizeNewLines()));
        }

        [Test]
        public void Linq27()
        {
            Assert.That(render(@"
(defn linq27 []
  (let ( (numbers [5 4 1 3 9 8 6 7 2 0])
         (i 0) (later-numbers) )
    (setq later-numbers (skip-while #(>= % (f++ i)) numbers))
    (println ""All elements starting from first element less than its position:"")
    (doseq (n later-numbers) (println n))
  ))
(linq27)"), 
                
                Does.StartWith(@"
All elements starting from first element less than its position:
1
3
9
8
6
7
2
0
".NormalizeNewLines()));
        }

        [Test]
        public void Linq28()
        {
            Assert.That(render(@"
(defn linq28 []
  (let ( (words [""cherry"" ""apple"" ""blueberry""])
         (sorted-words) )
    (setq sorted-words (sort words))
    (println ""The sorted list of words:"")
    (doseq (w sorted-words) (println w))
  ))
(linq28)"), 
                
                Does.StartWith(@"
The sorted list of words:
apple
blueberry
cherry
".NormalizeNewLines()));
        }

        [Test]
        public void Linq29()
        {
            Assert.That(render(@"
(defn linq29 []
  (let ( (words [""cherry"" ""apple"" ""blueberry""])
         (sorted-words) )
    (setq sorted-words (sort-by count words))
    (println ""The sorted list of words (by length):"")
    (doseq (w sorted-words) (println w))
  ))
(linq29)"), 
                
                Does.StartWith(@"
The sorted list of words (by length):
apple
cherry
blueberry
".NormalizeNewLines()));
        }

        [Test]
        public void Linq30()
        {
            Assert.That(render(@"
(defn linq30 []
  (let ( (sorted-products (sort-by .ProductName products-list)) )
    (doseq (p sorted-products) (dump-inline p))
  ))
(linq30)"), 
                
                Does.StartWith(@"
{ProductId:17,ProductName:Alice Mutton,Category:Meat/Poultry,UnitPrice:39,UnitsInStock:0}
{ProductId:3,ProductName:Aniseed Syrup,Category:Condiments,UnitPrice:10,UnitsInStock:13}
{ProductId:40,ProductName:Boston Crab Meat,Category:Seafood,UnitPrice:18.4,UnitsInStock:123}
{ProductId:60,ProductName:Camembert Pierrot,Category:Dairy Products,UnitPrice:34,UnitsInStock:19}
{ProductId:18,ProductName:Carnarvon Tigers,Category:Seafood,UnitPrice:62.5,UnitsInStock:42}
".NormalizeNewLines()).Or.StartsWith(@"
{UnitsInStock:0,ProductName:Alice Mutton,UnitPrice:39,Category:Meat/Poultry,ProductId:17}
{UnitsInStock:13,ProductName:Aniseed Syrup,UnitPrice:10,Category:Condiments,ProductId:3}
{UnitsInStock:123,ProductName:Boston Crab Meat,UnitPrice:18.4,Category:Seafood,ProductId:40}
{UnitsInStock:19,ProductName:Camembert Pierrot,UnitPrice:34,Category:Dairy Products,ProductId:60}
{UnitsInStock:42,ProductName:Carnarvon Tigers,UnitPrice:62.5,Category:Seafood,ProductId:18}
".NormalizeNewLines())); // different ordering in .NET Core 
        }

        [Test]
        public void Linq31()
        {
            Assert.That(render(@"
(defn linq31 []
  (let ( (words [""aPPLE"" ""AbAcUs"" ""bRaNcH"" ""BlUeBeRrY"" ""ClOvEr"" ""cHeRry""])
         (sorted-words) )
    (setq sorted-words (sort-by it (CaseInsensitiveComparer.) words))
    (doseq (w sorted-words) (println w))
  ))
(linq31)"), 
                
                Does.StartWith(@"
AbAcUs
aPPLE
BlUeBeRrY
bRaNcH
cHeRry
ClOvEr
".NormalizeNewLines()));
        }

        [Test]
        public void Linq32()
        {
            Assert.That(render(@"
(defn linq32 []
  (let ( (dbls [1.7 2.3 1.9 4.1 2.9])
         (sorted-doubles) )
    (setq sorted-doubles (reverse (sort dbls)))
    (println ""The doubles from highest to lowest:"")
    (doseq (d sorted-doubles) (println d))
  ))
(linq32)"), 
                
                Does.StartWith(@"
The doubles from highest to lowest:
4.1
2.9
2.3
1.9
1.7
".NormalizeNewLines()));
        }

        [Test]
        public void linq33()
        {
            Assert.That(render(@"
(defn linq33 []
  (let ( (sorted-products (reverse (sort-by .UnitsInStock products-list))) )
        (doseq (p sorted-products) (dump-inline p))
    ))
(linq33)"), 
                
                Does.StartWith(@"
{ProductId:75,ProductName:Rhönbräu Klosterbier,Category:Beverages,UnitPrice:7.75,UnitsInStock:125}
{ProductId:40,ProductName:Boston Crab Meat,Category:Seafood,UnitPrice:18.4,UnitsInStock:123}
{ProductId:6,ProductName:Grandma's Boysenberry Spread,Category:Condiments,UnitPrice:25,UnitsInStock:120}
{ProductId:55,ProductName:Pâté chinois,Category:Meat/Poultry,UnitPrice:24,UnitsInStock:115}
{ProductId:61,ProductName:Sirop d'érable,Category:Condiments,UnitPrice:28.5,UnitsInStock:113}
".NormalizeNewLines()).Or.StartsWith(@"
{UnitsInStock:125,ProductName:Rhönbräu Klosterbier,UnitPrice:7.75,Category:Beverages,ProductId:75}
{UnitsInStock:123,ProductName:Boston Crab Meat,UnitPrice:18.4,Category:Seafood,ProductId:40}
{UnitsInStock:120,ProductName:Grandma's Boysenberry Spread,UnitPrice:25,Category:Condiments,ProductId:6}
{UnitsInStock:115,ProductName:Pâté chinois,UnitPrice:24,Category:Meat/Poultry,ProductId:55}
{UnitsInStock:113,ProductName:Sirop d'érable,UnitPrice:28.5,Category:Condiments,ProductId:61}
".NormalizeNewLines()).Or.StartsWith(@"
{UnitsInStock:125,ProductId:75,ProductName:Rhönbräu Klosterbier,Category:Beverages,UnitPrice:7.75}
{UnitsInStock:123,ProductId:40,ProductName:Boston Crab Meat,Category:Seafood,UnitPrice:18.4}
{UnitsInStock:120,ProductId:6,ProductName:Grandma's Boysenberry Spread,Category:Condiments,UnitPrice:25}
{UnitsInStock:115,ProductId:55,ProductName:Pâté chinois,Category:Meat/Poultry,UnitPrice:24}
{UnitsInStock:113,ProductId:61,ProductName:Sirop d'érable,Category:Condiments,UnitPrice:28.5}
".NormalizeNewLines()));
        }

        [Test]
        public void linq34()
        {
            Assert.That(render(@"
(defn linq34 []
  (let ( (words [""aPPLE"" ""AbAcUs"" ""bRaNcH"" ""BlUeBeRrY"" ""ClOvEr"" ""cHeRry""])
         (sorted-words) )
    (setq sorted-words (order-by [{ :comparer (CaseInsensitiveComparer.) :desc true }] words))
    (doseq (w sorted-words) (println w))
  ))
(linq34)"), 
                
                Does.StartWith(@"
ClOvEr
cHeRry
bRaNcH
BlUeBeRrY
aPPLE
AbAcUs
".NormalizeNewLines()));
        }

        [Test]
        public void linq35()
        {
            Assert.That(render(@"
(defn linq35 []
  (let ( (digits [""zero"" ""one"" ""two"" ""three"" ""four"" ""five"" ""six"" ""seven"" ""eight"" ""nine""]) 
         (i 0) (sorted-digits) )
    (setq sorted-digits (order-by [#(count %) it] digits ))
    (println ""Sorted digits:"")
    (doseq (d sorted-digits) (println d))
  ))
(linq35)"), 
                
                Does.StartWith(@"
Sorted digits:
one
six
two
five
four
nine
zero
eight
seven
three
".NormalizeNewLines()));
        }

        [Test]
        public void linq36()
        {
            Assert.That(render(@"
(defn linq36 []
  (let ( (words [""aPPLE"" ""AbAcUs"" ""bRaNcH"" ""BlUeBeRrY"" ""ClOvEr"" ""cHeRry""]) 
         (sorted-words) )
    (setq sorted-words (order-by [#(count %) { :comparer (CaseInsensitiveComparer.) }] words))
    (doseq (w sorted-words) (println w))
  ))
(linq36)"), 
                
                Does.StartWith(@"
aPPLE
AbAcUs
bRaNcH
cHeRry
ClOvEr
BlUeBeRrY
".NormalizeNewLines()));
        }

        [Test]
        public void linq37()
        {
            Assert.That(render(@"
(defn linq37 []
  (let ( (sorted-products (order-by [ #(.Category %) { :key #(.UnitPrice %) :desc true } ] products-list)) )
    (doseq (p sorted-products) (dump-inline p))
  ))
(linq37)"), 
                
                Does.StartWith(@"
{ProductId:38,ProductName:Côte de Blaye,Category:Beverages,UnitPrice:263.5,UnitsInStock:17}
{ProductId:43,ProductName:Ipoh Coffee,Category:Beverages,UnitPrice:46,UnitsInStock:17}
{ProductId:2,ProductName:Chang,Category:Beverages,UnitPrice:19,UnitsInStock:17}
{ProductId:1,ProductName:Chai,Category:Beverages,UnitPrice:18,UnitsInStock:39}
{ProductId:35,ProductName:Steeleye Stout,Category:Beverages,UnitPrice:18,UnitsInStock:20}
{ProductId:39,ProductName:Chartreuse verte,Category:Beverages,UnitPrice:18,UnitsInStock:69}
{ProductId:76,ProductName:Lakkalikööri,Category:Beverages,UnitPrice:18,UnitsInStock:57}
{ProductId:70,ProductName:Outback Lager,Category:Beverages,UnitPrice:15,UnitsInStock:15}
{ProductId:34,ProductName:Sasquatch Ale,Category:Beverages,UnitPrice:14,UnitsInStock:111}
".NormalizeNewLines()).Or.StartsWith(@"
{UnitsInStock:17,ProductName:Côte de Blaye,UnitPrice:263.5,Category:Beverages,ProductId:38}
{UnitsInStock:17,ProductName:Ipoh Coffee,UnitPrice:46,Category:Beverages,ProductId:43}
{UnitsInStock:17,ProductName:Chang,UnitPrice:19,Category:Beverages,ProductId:2}
{UnitsInStock:39,ProductName:Chai,UnitPrice:18,Category:Beverages,ProductId:1}
{UnitsInStock:20,ProductName:Steeleye Stout,UnitPrice:18,Category:Beverages,ProductId:35}
{UnitsInStock:69,ProductName:Chartreuse verte,UnitPrice:18,Category:Beverages,ProductId:39}
{UnitsInStock:57,ProductName:Lakkalikööri,UnitPrice:18,Category:Beverages,ProductId:76}
{UnitsInStock:15,ProductName:Outback Lager,UnitPrice:15,Category:Beverages,ProductId:70}
{UnitsInStock:111,ProductName:Sasquatch Ale,UnitPrice:14,Category:Beverages,ProductId:34}
".NormalizeNewLines())); // different ordering in .NET Core 
        }

        [Test]
        public void linq38()
        {
            Assert.That(render(@"
(defn linq38 []
  (let ( (words [""aPPLE"" ""AbAcUs"" ""bRaNcH"" ""BlUeBeRrY"" ""ClOvEr"" ""cHeRry""]) 
         (sorted-words) )
  
    (setq sorted-words (order-by [ #(count %) { :comparer (CaseInsensitiveComparer.) :desc true } ] words))
    (doseq (w sorted-words) (println w))
  ))
(linq38)"), 
                
                Does.StartWith(@"
aPPLE
ClOvEr
cHeRry
bRaNcH
AbAcUs
BlUeBeRrY
".NormalizeNewLines()));
        }

        [Test]
        public void linq39()
        {
            Assert.That(render(@"
(defn linq39 []
  (let ( (digits [""zero"" ""one"" ""two"" ""three"" ""four"" ""five"" ""six"" ""seven"" ""eight"" ""nine""])
         (sorted-digits) )
    (setq sorted-digits (reverse (where #(= (:1 %) (:0 ""i"")) digits)) )
    (println ""A backwards list of the digits with a second character of 'i':"")
    (doseq (d sorted-digits) (println d))
  ))
(linq39)"), 
                
                Does.StartWith(@"
A backwards list of the digits with a second character of 'i':
nine
eight
six
five
".NormalizeNewLines()));
        }

        [Test]
        public void linq40()
        {
            Assert.That(render(@"
(defn linq40 []
  (let ( (numbers [5 4 1 3 9 8 6 7 2 0]) 
         (number-groups) )
    (setq number-groups 
        (map (fn [g] { :remainder (.Key g) :numbers g }) (group-by #(mod % 5) numbers)))
    (doseq (g number-groups)
        (println ""Numbers with a remainder of "" (:remainder g) "" when divided by 5:"")
        (doseq (n (:numbers g)) (println n)))
  ))
(linq40)"), 
                
                Does.StartWith(@"
Numbers with a remainder of 0 when divided by 5:
5
0
Numbers with a remainder of 4 when divided by 5:
4
9
Numbers with a remainder of 1 when divided by 5:
1
6
Numbers with a remainder of 3 when divided by 5:
3
8
Numbers with a remainder of 2 when divided by 5:
7
2
".NormalizeNewLines()));
        }

        [Test]
        public void linq41()
        {
            Assert.That(render(@"
(defn linq41 []
  (let ( (words [""blueberry"" ""chimpanzee"" ""abacus"" ""banana"" ""apple"" ""cheese""])
         (word-groups) )
    (setq word-groups 
        (map (fn [g] {:first-letter (.Key g) :words g}) (group-by #(nth % 0) words) ))
    (doseq (g word-groups)
        (println ""Words that start with the letter: "" (:first-letter g))
        (doseq (w (:words g)) (println w)))
  ))
(linq41)"), 
                
                Does.StartWith(@"
Words that start with the letter: b
blueberry
banana
Words that start with the letter: c
chimpanzee
cheese
Words that start with the letter: a
abacus
apple
".NormalizeNewLines()));
        }

        [Test]
        public void linq42()
        {
            Assert.That(render(@"
(defn linq42 []
  (let ( (order-groups 
      (map (fn [g] {:category (.Key g), :products g}) (group-by :category products-list))) )
    (doseq (x order-groups) (dump-inline x))
  ))
(linq42)"), 
                
                Does.StartWith(@"
{category:Beverages,products:[{ProductId:1,ProductName:Chai,Category:Beverages,UnitPrice:18,UnitsInStock:39},{ProductId:2,ProductName:Chang,Category:Beverages,UnitPrice:19,UnitsInStock:17},{ProductId:24,ProductName:Guaraná Fantástica,Category:Beverages,UnitPrice:4.5,UnitsInStock:20},{ProductId:34,ProductName:Sasquatch Ale,Category:Beverages,UnitPrice:14,UnitsInStock:111},{ProductId:35,ProductName:Steeleye Stout,Category:Beverages,UnitPrice:18,UnitsInStock:20},{ProductId:38,ProductName:Côte de Blaye,Category:Beverages,UnitPrice:263.5,UnitsInStock:17},{ProductId:39,ProductName:Chartreuse verte,Category:Beverages,UnitPrice:18,UnitsInStock:69},{ProductId:43,ProductName:Ipoh Coffee,Category:Beverages,UnitPrice:46,UnitsInStock:17},{ProductId:67,ProductName:Laughing Lumberjack Lager,Category:Beverages,UnitPrice:14,UnitsInStock:52},{ProductId:70,ProductName:Outback Lager,Category:Beverages,UnitPrice:15,UnitsInStock:15},{ProductId:75,ProductName:Rhönbräu Klosterbier,Category:Beverages,UnitPrice:7.75,UnitsInStock:125},{ProductId:76,ProductName:Lakkalikööri,Category:Beverages,UnitPrice:18,UnitsInStock:57}]}
".NormalizeNewLines()).Or.StartsWith(@"
{category:Beverages,products:[{UnitsInStock:39,ProductName:Chai,UnitPrice:18,Category:Beverages,ProductId:1},{UnitsInStock:17,ProductName:Chang,UnitPrice:19,Category:Beverages,ProductId:2},{UnitsInStock:20,ProductName:Guaraná Fantástica,UnitPrice:4.5,Category:Beverages,ProductId:24},{UnitsInStock:111,ProductName:Sasquatch Ale,UnitPrice:14,Category:Beverages,ProductId:34},{UnitsInStock:20,ProductName:Steeleye Stout,UnitPrice:18,Category:Beverages,ProductId:35},{UnitsInStock:17,ProductName:Côte de Blaye,UnitPrice:263.5,Category:Beverages,ProductId:38},{UnitsInStock:69,ProductName:Chartreuse verte,UnitPrice:18,Category:Beverages,ProductId:39},{UnitsInStock:17,ProductName:Ipoh Coffee,UnitPrice:46,Category:Beverages,ProductId:43},{UnitsInStock:52,ProductName:Laughing Lumberjack Lager,UnitPrice:14,Category:Beverages,ProductId:67},{UnitsInStock:15,ProductName:Outback Lager,UnitPrice:15,Category:Beverages,ProductId:70},{UnitsInStock:125,ProductName:Rhönbräu Klosterbier,UnitPrice:7.75,Category:Beverages,ProductId:75},{UnitsInStock:57,ProductName:Lakkalikööri,UnitPrice:18,Category:Beverages,ProductId:76}]}
".NormalizeNewLines())); // different ordering in .NET Core 
        }

        [Test]
        public void linq43()
        {
            Assert.That(render(@"
(defn linq43 []
  (let ( (customer-order-groups
      (map (fn [c] {
        :company-name (.CompanyName c)
        :year-groups  (map (fn [yg] {
                :year (.Key yg)
                :month-groups (map (fn [mg] {
                        :month  (.Key mg)
                        :orders mg
                    }) (group-by #(.Month (.OrderDate %)) yg))
            }) (group-by (fn [o] (.Year (.OrderDate o))) (.Orders c)))
      }) customers-list)) )
    (dump customer-order-groups)
  ))
(linq43)"), 
                
                Does.StartWith(@"
[
	{
		company-name: Alfreds Futterkiste,
		year-groups: 
		[
			{
				year: 1997,
				month-groups: 
				[
					{
						month: 8,
						orders: 
						[
							{
								OrderId: 10643,
								OrderDate: 1997-08-25,
								Total: 814.5
							}
						]
					},
					{
						month: 10,
						orders: 
						[
							{
								OrderId: 10692,
								OrderDate: 1997-10-03,
								Total: 878
							},
							{
								OrderId: 10702,
								OrderDate: 1997-10-13,
								Total: 330
							}
						]
					}
				]
			},
			{
				year: 1998,
				month-groups: 
				[
					{
						month: 1,
						orders: 
						[
							{
								OrderId: 10835,
								OrderDate: 1998-01-15,
								Total: 845.8
							}
						]
					},
					{
						month: 3,
						orders: 
						[
							{
								OrderId: 10952,
								OrderDate: 1998-03-16,
								Total: 471.2
							}
						]
					},
					{
						month: 4,
						orders: 
						[
							{
								OrderId: 11011,
								OrderDate: 1998-04-09,
								Total: 933.5
							}
						]
					}
				]
			}
		]
	},
".NormalizeNewLines()));
        }

        [Test]
        public void linq44()
        {
            Assert.That(render(@"
(defn linq44 []
  (let ( (anagrams [""from   "" "" salt"" "" earn "" ""  last   "" "" near "" "" form  ""]) 
         (order-groups) )
    (setq order-groups (group-by .Trim { :comparer (AnagramEqualityComparer.) } anagrams))
    (doseq (x order-groups) (dump-inline x))
  ))
(linq44)"), 
                
                Does.StartWith(@"
[from   , form  ]
[ salt,  last   ]
[ earn , near ]
".NormalizeNewLines()));
        }

        [Test]
        public void linq44_inline()
        {
            Assert.That(render(@"
(defn linq44 []
  (let ( (anagrams [""from   "" "" salt"" "" earn "" ""  last   "" "" near "" "" form  ""]) 
         (order-groups) )
    (setq order-groups (group-by #((/C ""String(char[])"") (sort (.ToCharArray (.Trim %)))) anagrams))
    (doseq (x order-groups) (dump-inline x))
  ))
(linq44)"), 
                
                Does.StartWith(@"
[from   , form  ]
[ salt,  last   ]
[ earn , near ]
".NormalizeNewLines()));
        }

        [Test]
        public void linq45()
        {
            Assert.That(render(@"
(defn linq45 []
  (let ( (anagrams [""from   "" "" salt"" "" earn "" ""  last   "" "" near "" "" form  ""]) 
         (order-groups) )
    (setq order-groups (group-by .Trim { :comparer (AnagramEqualityComparer.) :map upper-case } anagrams))
    (doseq (x order-groups) (dump-inline x))
  ))
(linq45)"), 
                
                Does.StartWith(@"
[FROM   , FORM  ]
[ SALT,  LAST   ]
[ EARN , NEAR ]
".NormalizeNewLines()));
        }

        [Test]
        public void linq46()
        {
            Assert.That(render(@"
(defn linq46 []
  (let ( (factors-of-300 [2, 2, 3, 5, 5])
         (unique-factors) )
    (setq unique-factors (/distinct factors-of-300))
    (println ""Prime factors of 300:"")
    (doseq (n unique-factors) (println n))
  ))
(linq46)"), 
                
                Does.StartWith(@"
Prime factors of 300:
2
3
5
".NormalizeNewLines()));
        }

        [Test]
        public void linq47()
        {
            Assert.That(render(@"
(defn linq47 []
  (let ( (category-names (/distinct (map .Category products-list))) )
    (println ""Category names:"")
    (doseq (c category-names) (println c))
  ))
(linq47)"), 
                
                Does.StartWith(@"
Category names:
Beverages
Condiments
Produce
Meat/Poultry
Seafood
Dairy Products
Confections
Grains/Cereals
".NormalizeNewLines()));
        }

        [Test]
        public void linq48()
        {
            Assert.That(render(@"
(defn linq48 []
  (let ( (numbers-a [0 2 4 5 6 8 9]) 
         (numbers-b [1 3 5 7 8])
         (unique-numbers) )
        
    (setq unique-numbers (/union numbers-a numbers-b))
    (println ""Unique numbers from both arrays:"")
    (doseq (n unique-numbers) (println n))
  ))
(linq48)"), 
                
                Does.StartWith(@"
Unique numbers from both arrays:
0
2
4
5
6
8
9
1
3
7
".NormalizeNewLines()));
        }

        [Test]
        public void linq49()
        {
            Assert.That(render(@"
(defn linq49 []
  (let ( (product-first-chars  (map #(nth (.ProductName %) 0) products-list))
         (customer-first-chars (map #(nth (.CompanyName %) 0) customers-list))
         (unique-first-chars) )
        
    (setq unique-first-chars (/union product-first-chars customer-first-chars))
    (println ""Unique first letters from Product names and Customer names:"")
    (doseq (x unique-first-chars) (println x))
  ))
(linq49)"), 
                
                Does.StartWith(@"
Unique first letters from Product names and Customer names:
C
A
G
U
N
M
I
Q
K
T
P
S
R
B
J
Z
V
F
E
W
L
O
D
H
".NormalizeNewLines()));
        }

        [Test]
        public void linq50()
        {
            Assert.That(render(@"
(defn linq50 []
  (let ( (numbers-a [0 2 4 5 6 8 9])
         (numbers-b [1 3 5 7 8]) )
    (setq common-numbers (intersect numbers-a numbers-b))
    (println ""Common numbers shared by both arrays:"")
    (doseq (n common-numbers) (println n))
  ))
(linq50)"), 
                
                Does.StartWith(@"
Common numbers shared by both arrays:
5
8
".NormalizeNewLines()));
        }

        [Test]
        public void linq51()
        {
            Assert.That(render(@"
(defn linq51 []
  (let ( (product-first-chars  (map #(nth (.ProductName %) 0) products-list))
         (customer-first-chars (map #(nth (.CompanyName %) 0) customers-list))
         (common-first-chars) )
    (setq common-first-chars (intersect product-first-chars customer-first-chars))
    (println ""Common first letters from Product names and Customer names:"")
    (doseq (x common-first-chars) (println x))
  ))
(linq51)"), 
                
                Does.StartWith(@"
Common first letters from Product names and Customer names:
C
A
G
N
M
I
Q
K
T
P
S
R
B
V
F
E
W
L
O
".NormalizeNewLines()));
        }

        [Test]
        public void linq52()
        {
            Assert.That(render(@"
(defn linq52 []
  (let ( (numbers-a [0 2 4 5 6 8 9])
         (numbers-b [1 3 5 7 8]) 
         (a-only-numbers) )
    (setq a-only-numbers (/except numbers-a numbers-b))
    (println ""Numbers in first array but not second array:"")
    (doseq (n a-only-numbers) (println n))
  ))
(linq52)"), 
                
                Does.StartWith(@"
Numbers in first array but not second array:
0
2
4
6
9
".NormalizeNewLines()));
        }

        [Test]
        public void linq53()
        {
            Assert.That(render(@"
(defn linq53 []
  (let ( (product-first-chars  (map #(nth (.ProductName %) 0) products-list))
         (customer-first-chars (map #(nth (.CompanyName %) 0) customers-list))
         (product-only-first-chars) )

    (setq product-only-first-chars (/except product-first-chars customer-first-chars))
    (println ""First letters from Product names, but not from Customer names:"")
    (doseq (x  product-only-first-chars) (println x))
  ))
(linq53)"), 
                
                Does.StartWith(@"
First letters from Product names, but not from Customer names:
U
J
Z
".NormalizeNewLines()));
        }

        [Test]
        public void linq54()
        {
            Assert.That(render(@"
(defn linq54 []
  (let ( (dbls [1.7 2.3 1.9 4.1 2.9])
         (sorted-doubles) )
    (setq sorted-doubles (reverse (sort dbls)))
    (println ""Every other double from highest to lowest:"")
    (doseq (d (/step sorted-doubles { :by 2 })) (println d))
  ))
(linq54)"), 
                
                Does.StartWith(@"
Every other double from highest to lowest:
4.1
2.3
1.7
".NormalizeNewLines()));
        }

        [Test]
        public void linq55()
        {
            Assert.That(render(@"
(defn linq55 []
  (let ( (words [""cherry"" ""apple"" ""blueberry""]) 
         (sorted-words) )
    (setq sorted-words (to-list (sort words)))
    (println ""The sorted word list:"")
    (doseq (w sorted-words) (println w))
  ))
(linq55)"), 
                
                Does.StartWith(@"
The sorted word list:
apple
blueberry
cherry
".NormalizeNewLines()));
        }

        [Test]
        public void linq56()
        {
            Assert.That(render(@"
(defn linq56 []
  (let ( (sorted-records [{:name ""Alice"", :score 50}
                          {:name ""Bob"",   :score 40}
                          {:name ""Cathy"", :score 45}]) 
          (sorted-records-dict) )
    (setq sorted-records-dict (to-dictionary :name sorted-records))
    (println ""Bob's score: "" (:score (:""Bob"" sorted-records-dict)))
  ))
(linq56)"), 
                
                Does.StartWith(@"
Bob's score: 40
".NormalizeNewLines()));
        }

        [Test]
        public void linq57()
        {
            Assert.That(render(@"
(defn linq57 []
  (let ( (numbers [nil 1.0 ""two"" 3 ""four"" 5 ""six"" 7.0]) 
         (dbls) )
    (setq dbls (/of numbers { :type ""Double"" }))
    (println ""Numbers stored as doubles:"")
    (doseq (d dbls) (println d))
  ))
(linq57)"), 
                
                Does.StartWith(@"
Numbers stored as doubles:
1
7
".NormalizeNewLines()));
        }

        [Test]
        public void linq58()
        {
            Assert.That(render(@"
(defn linq58 []
  (let ( (product-12 (first (where #(= (.ProductId %) 12) products-list)) ) )
    (dump-inline product-12)
  ))
(linq58)"), 
                
                Does.StartWith(@"
{ProductId:12,ProductName:Queso Manchego La Pastora,Category:Dairy Products,UnitPrice:38,UnitsInStock:86}
".NormalizeNewLines()).Or.StartsWith(@"
{UnitsInStock:86,ProductName:Queso Manchego La Pastora,UnitPrice:38,Category:Dairy Products,ProductId:12}
".NormalizeNewLines())); // different ordering in .NET Core 
        }

        [Test]
        public void linq59()
        {
            Assert.That(render(@"
(defn linq59 []
  (let ( (strings [""zero"" ""one"" ""two"" ""three"" ""four"" ""five"" ""six"" ""seven"" ""eight"" ""nine""]) 
         (starts-with-o) )
    (setq starts-with-o (first (where #(/startsWith % ""o"") strings)))
    (println ""A string starting with 'o': "" starts-with-o)
  ))
(linq59)"),

                Does.StartWith(@"
A string starting with 'o': one
".NormalizeNewLines()));
        }

        [Test]
        public void linq61()
        {
            Assert.That(render(@"
(defn linq61 []
  (let ( (numbers []) (first-num-or-default) )
    (setq first-num-or-default (or (first numbers) 0))
    (println first-num-or-default)
  ))
(linq61)"),

                Does.StartWith(@"
0
".NormalizeNewLines()));
        }

        [Test]
        public void linq62()
        {
            Assert.That(render(@"
(defn linq62 []
  (let ( (product-789 (first (where #(= (.ProductId %) 789) products-list) )) )
    (println ""Product 789 exists: "" (not= product-789 nil))
  ))
(linq62)"),

                Does.StartWith(@"
Product 789 exists: False
".NormalizeNewLines()));
        }

        [Test]
        public void linq64()
        {
            Assert.That(render(@"
(defn linq64 []
  (let ( (numbers [5 4 1 3 9 8 6 7 2 0]) (fourth-low-num) )
    (setq fourth-low-num (nth (where #(> % 5) numbers) 1))
    (println ""Second number > 5: "" fourth-low-num)
  ))
(linq64)"),

                Does.StartWith(@"
Second number > 5: 8
".NormalizeNewLines()));
        }

        [Test]
        public void linq65()
        {
            Assert.That(render(@"
(defn linq65 []
  (let ( (numbers (map (fn [n] { 
            :number n 
            :odd-even (if (odd? n) ""odd"" ""even"") 
        }) (range 100 151))) )
    (doseq (n numbers) 
      (println ""The number "" (:number n) "" is "" (:odd-even n)))
  ))
(linq65)"),

                Does.StartWith(@"
The number 100 is even
The number 101 is odd
The number 102 is even
The number 103 is odd
The number 104 is even
The number 105 is odd
The number 106 is even
The number 107 is odd
The number 108 is even
The number 109 is odd
The number 110 is even
".NormalizeNewLines()));
        }

        [Test]
        public void linq66()
        {
            Assert.That(render(@"
(defn linq66 []
  (let ( (numbers (/repeat 7 10)) )
    (doseq (n numbers) (println n))))
(linq66)"),

                Does.StartWith(@"
7
7
7
7
7
7
7
7
7
7
".NormalizeNewLines()));
        }

        [Test]
        public void linq67()
        {
            Assert.That(render(@"
(defn linq67 []
  (let ( (words [""believe"" ""relief"" ""receipt"" ""field""]) 
         (i-after-e) )
    (setq i-after-e (any? #(.Contains % ""ie"") words))
    (println ""There is a word that contains in the list that contains 'ei': "" i-after-e)
  ))
(linq67)"),

                Does.StartWith(@"
There is a word that contains in the list that contains 'ei': True
".NormalizeNewLines()));
        }

        [Test]
        public void linq69()
        {
            Assert.That(render(@"
(defn linq69 []
  (let ( (product-groups  
            (map #(it { :category (.Key %), :products % })
                (where #(any? (fn [p] (= (.UnitsInStock p) 0)) %) 
                    (group-by .Category products-list)))) )
    (dump product-groups)
  ))
(linq69)"),

                Does.StartWith(@"[
	{
		category: Condiments,
		products: 
		[
			{
				ProductId: 3,
				ProductName: Aniseed Syrup,
				Category: Condiments,
				UnitPrice: 10,
				UnitsInStock: 13
			},
			{
				ProductId: 4,
				ProductName: Chef Anton's Cajun Seasoning,
				Category: Condiments,
				UnitPrice: 22,
				UnitsInStock: 53
			},
			{
				ProductId: 5,
				ProductName: Chef Anton's Gumbo Mix,
				Category: Condiments,
				UnitPrice: 21.35,
				UnitsInStock: 0
			},
".NormalizeNewLines()).Or.StartsWith(@"[
	{
		category: Condiments,
		products: 
		[
			{
				UnitsInStock: 13,
				ProductName: Aniseed Syrup,
				UnitPrice: 10,
				Category: Condiments,
				ProductId: 3
			},".NormalizeNewLines()));
        }

        [Test]
        public void linq70()
        {
            Assert.That(render(@"
(defn linq70 []
  (let ( (numbers [1 11 3 19 41 65 19])
         (only-odd) )
    (setq only-odd (all? odd? numbers))
    (println ""The list contains only odd numbers: "" only-odd)
  ))
(linq70)"),

                Does.StartWith(@"
The list contains only odd numbers: True
".NormalizeNewLines()));
        }

        [Test]
        public void linq72()
        {
            Assert.That(render(@"
(defn linq72 []
  (let ( (product-groups  
            (map #(it { :category (.Key %), :products % })
                (where #(all? (fn [p] (> (.UnitsInStock p) 0)) %) 
                    (group-by .Category products-list)))) )
    (dump product-groups)
  ))
(linq72)"),

                Does.StartWith(@"[
	{
		category: Beverages,
		products: 
		[
			{
				ProductId: 1,
				ProductName: Chai,
				Category: Beverages,
				UnitPrice: 18,
				UnitsInStock: 39
			},
			{
				ProductId: 2,
				ProductName: Chang,
				Category: Beverages,
				UnitPrice: 19,
				UnitsInStock: 17
			},
			{
				ProductId: 24,
				ProductName: Guaraná Fantástica,
				Category: Beverages,
				UnitPrice: 4.5,
				UnitsInStock: 20
			},
".NormalizeNewLines()).Or.StartsWith(@"[
	{
		category: Beverages,
		products: 
		[
			{
				UnitsInStock: 39,
				ProductName: Chai,
				UnitPrice: 18,
				Category: Beverages,
				ProductId: 1
			},".NormalizeNewLines()));
        }

        [Test]
        public void linq73()
        {
            Assert.That(render(@"
(defn linq73 []
  (let ( (factors-of-300 [2 2 3 5 5]) 
         (unique-factors) )
    (setq unique-factors (count (/distinct factors-of-300)))
    (println ""There are "" unique-factors "" unique factors of 300."")
  ))
(linq73)"),

                Does.StartWith(@"
There are 3 unique factors of 300.
".NormalizeNewLines()));
        }

        [Test]
        public void linq74()
        {
            Assert.That(render(@"
(defn linq74 []
  (let ( (numbers [4 5 1 3 9 0 6 7 2 0])
         (odd-numbers) )
    (setq odd-numbers (count (where odd? numbers)) )
    (println ""There are "" odd-numbers "" odd numbers in the list."")
  ))
(linq74)"),

                Does.StartWith(@"
There are 5 odd numbers in the list.
".NormalizeNewLines()));
        }
        
        [Test]
        public void linq76()
        {
            Assert.That(render(@"
(defn linq76 []
  (let ( (order-counts 
          (map #(it { 
                :customer-id (.CustomerId %) 
                :order-count (count (.Orders %)) 
             }) customers-list)) )
  (doseq (x order-counts) (dump-inline x))
))
(linq76)"),

                Does.StartWith(@"
{customer-id:ALFKI,order-count:6}
{customer-id:ANATR,order-count:4}
{customer-id:ANTON,order-count:7}
{customer-id:AROUT,order-count:13}
{customer-id:BERGS,order-count:18}
{customer-id:BLAUS,order-count:7}
{customer-id:BLONP,order-count:11}
".NormalizeNewLines()));
        }
        
        [Test]
        public void linq77()
        {
            Assert.That(render(@"
(defn linq77 []
  (let ( (category-counts
            (map #(it {
                  :category (.Key %)
                  :product-count (count %)
              })
            (group-by .Category products-list))) )
    (doseq (x category-counts) (dump-inline x))
  ))
(linq77)"),

                Does.StartWith(@"
{category:Beverages,product-count:12}
{category:Condiments,product-count:12}
{category:Produce,product-count:5}
{category:Meat/Poultry,product-count:6}
{category:Seafood,product-count:12}
{category:Dairy Products,product-count:10}
{category:Confections,product-count:13}
{category:Grains/Cereals,product-count:7}
".NormalizeNewLines()));
        }
        
        [Test]
        public void linq78()
        {
            Assert.That(render(@"
(defn linq78 []
  (let ( (numbers [5 4 1 3 9 8 6 7 2 0]) )
    (setq num-sum (reduce + numbers))
    (println ""The sum of the numbers is "" num-sum)
  ))
(linq78)"),

                Does.StartWith(@"
The sum of the numbers is 45
".NormalizeNewLines()));
        }
        
        [Test]
        public void linq79()
        {
            Assert.That(render(@"
(defn linq79 []
  (let ( (words [""cherry"", ""apple"", ""blueberry""]) 
         (total-chars) )
    (setq total-chars (reduce + (map count words)))
    (println ""There are a total of "" total-chars "" characters in these words."")
  ))
(linq79)"),

                Does.StartWith(@"
There are a total of 20 characters in these words.
".NormalizeNewLines()));
        }
        
        [Test]
        public void linq80()
        {
            Assert.That(render(@"
(defn linq80 []
  (let ( (categories
             (map #(it {
                  :category (.Key %)
                  :total-units-in-stock (sum (map .UnitsInStock %))
                })
                (group-by .Category products-list))) )
    (doseq (x categories) (dump-inline x))
  ))
(linq80)"),

                Does.StartWith(@"
{category:Beverages,total-units-in-stock:559}
{category:Condiments,total-units-in-stock:507}
{category:Produce,total-units-in-stock:100}
{category:Meat/Poultry,total-units-in-stock:165}
{category:Seafood,total-units-in-stock:701}
{category:Dairy Products,total-units-in-stock:393}
{category:Confections,total-units-in-stock:386}
{category:Grains/Cereals,total-units-in-stock:308}
".NormalizeNewLines()));
        }
        
        [Test]
        public void linq81()
        {
            Assert.That(render(@"
(defn linq81 []
  (let ( (numbers [5 4 1 3 9 8 6 7 2 0]) 
         (min-num) )
    (setq min-num (apply min numbers))
    (println ""The minimum number is "" min-num)
  ))
(linq81)"),

                Does.StartWith(@"
The minimum number is 0
".NormalizeNewLines()));
        }
        
        [Test]
        public void linq82()
        {
            Assert.That(render(@"
(defn linq82 []
  (let ( (words [""cherry"", ""apple"", ""blueberry""])
         (shortest-word) )
    (setq shortest-word (apply min (map count words)))
    (println ""The shortest word is "" shortest-word "" characters long."")
  ))
(linq82)"),

                Does.StartWith(@"
The shortest word is 5 characters long.
".NormalizeNewLines()));
        }
        
        [Test]
        public void linq83()
        {
            Assert.That(render(@"
(defn linq83 []
  (let ( (categories
             (map #(it {
                  :category (.Key %)
                  :cheapest-price (apply min (map .UnitPrice %))
                })
              (group-by .Category products-list))) )
    (doseq (x categories) (dump-inline x))
  ))
(linq83)"),

                Does.StartWith(@"
{category:Beverages,cheapest-price:4.5}
{category:Condiments,cheapest-price:10}
{category:Produce,cheapest-price:10}
{category:Meat/Poultry,cheapest-price:7.45}
{category:Seafood,cheapest-price:6}
{category:Dairy Products,cheapest-price:2.5}
{category:Confections,cheapest-price:9.2}
{category:Grains/Cereals,cheapest-price:7}
".NormalizeNewLines()));
        }
        
        [Test]
        public void linq84()
        {
            Assert.That(render(@"
(defn linq84 []
  (let ( (categories
            (map (fn [g] (
                let ( (min-price (apply min (map .UnitPrice g))) )
                    {
                      :category (.Key g)
                      :cheapest-products (where #(= (.UnitPrice %) min-price) g) 
                    }))
                (group-by .Category products-list))) )
    (doseq (x categories) (dump-inline x))
  ))
(linq84)"),

                Does.StartWith(@"
{category:Beverages,cheapest-products:[{ProductId:24,ProductName:Guaraná Fantástica,Category:Beverages,UnitPrice:4.5,UnitsInStock:20}]}
{category:Condiments,cheapest-products:[{ProductId:3,ProductName:Aniseed Syrup,Category:Condiments,UnitPrice:10,UnitsInStock:13}]}
{category:Produce,cheapest-products:[{ProductId:74,ProductName:Longlife Tofu,Category:Produce,UnitPrice:10,UnitsInStock:4}]}
{category:Meat/Poultry,cheapest-products:[{ProductId:54,ProductName:Tourtière,Category:Meat/Poultry,UnitPrice:7.45,UnitsInStock:21}]}
{category:Seafood,cheapest-products:[{ProductId:13,ProductName:Konbu,Category:Seafood,UnitPrice:6,UnitsInStock:24}]}
{category:Dairy Products,cheapest-products:[{ProductId:33,ProductName:Geitost,Category:Dairy Products,UnitPrice:2.5,UnitsInStock:112}]}
{category:Confections,cheapest-products:[{ProductId:19,ProductName:Teatime Chocolate Biscuits,Category:Confections,UnitPrice:9.2,UnitsInStock:25}]}
{category:Grains/Cereals,cheapest-products:[{ProductId:52,ProductName:Filo Mix,Category:Grains/Cereals,UnitPrice:7,UnitsInStock:38}]}
".NormalizeNewLines()).Or.StartsWith(@"
{category:Beverages,cheapest-products:[{UnitsInStock:20,ProductName:Guaraná Fantástica,UnitPrice:4.5,Category:Beverages,ProductId:24}]}
{category:Condiments,cheapest-products:[{UnitsInStock:13,ProductName:Aniseed Syrup,UnitPrice:10,Category:Condiments,ProductId:3}]}
{category:Produce,cheapest-products:[{UnitsInStock:4,ProductName:Longlife Tofu,UnitPrice:10,Category:Produce,ProductId:74}]}
{category:Meat/Poultry,cheapest-products:[{UnitsInStock:21,ProductName:Tourtière,UnitPrice:7.45,Category:Meat/Poultry,ProductId:54}]}
{category:Seafood,cheapest-products:[{UnitsInStock:24,ProductName:Konbu,UnitPrice:6,Category:Seafood,ProductId:13}]}
{category:Dairy Products,cheapest-products:[{UnitsInStock:112,ProductName:Geitost,UnitPrice:2.5,Category:Dairy Products,ProductId:33}]}
{category:Confections,cheapest-products:[{UnitsInStock:25,ProductName:Teatime Chocolate Biscuits,UnitPrice:9.2,Category:Confections,ProductId:19}]}
{category:Grains/Cereals,cheapest-products:[{UnitsInStock:38,ProductName:Filo Mix,UnitPrice:7,Category:Grains/Cereals,ProductId:52}]}
".NormalizeNewLines()).Or.StartsWith(@"
{category:Beverages,cheapest-products:[{Category:Beverages,UnitPrice:4.5,ProductId:24,ProductName:Guaraná Fantástica,UnitsInStock:20}]}
{category:Condiments,cheapest-products:[{Category:Condiments,UnitPrice:10,ProductId:3,ProductName:Aniseed Syrup,UnitsInStock:13}]}
{category:Produce,cheapest-products:[{Category:Produce,UnitPrice:10,ProductId:74,ProductName:Longlife Tofu,UnitsInStock:4}]}
{category:Meat/Poultry,cheapest-products:[{Category:Meat/Poultry,UnitPrice:7.45,ProductId:54,ProductName:Tourtière,UnitsInStock:21}]}
{category:Seafood,cheapest-products:[{Category:Seafood,UnitPrice:6,ProductId:13,ProductName:Konbu,UnitsInStock:24}]}
{category:Dairy Products,cheapest-products:[{Category:Dairy Products,UnitPrice:2.5,ProductId:33,ProductName:Geitost,UnitsInStock:112}]}
{category:Confections,cheapest-products:[{Category:Confections,UnitPrice:9.2,ProductId:19,ProductName:Teatime Chocolate Biscuits,UnitsInStock:25}]}
{category:Grains/Cereals,cheapest-products:[{Category:Grains/Cereals,UnitPrice:7,ProductId:52,ProductName:Filo Mix,UnitsInStock:38}]}
".NormalizeNewLines()));
        }
        
        [Test]
        public void linq85()
        {
            Assert.That(render(@"
(defn linq85 []
  (let ( (numbers [5 4 1 3 9 8 6 7 2 0]) 
         (max-num) )
    (setq max-num (apply max numbers))
    (println ""The maximum number is "" max-num)
  ))
(linq85)"),

                Does.StartWith(@"
The maximum number is 9
".NormalizeNewLines()));
        }
        
        [Test]
        public void linq86()
        {
            Assert.That(render(@"
(defn linq82 []
  (let ( (words [""cherry"", ""apple"", ""blueberry""])
         (shortest-word) )
    (setq longest-word (apply max (map count words)))
    (println ""The longest word is "" longest-word "" characters long."")
  ))
(linq82)"),

                Does.StartWith(@"
The longest word is 9 characters long.
".NormalizeNewLines()));
        }
        
        [Test]
        public void linq87()
        {
            Assert.That(render(@"
(defn linq87 []
  (let ( (categories
             (map #(it {
                  :category (.Key %)
                  :most-expensive-price (apply max (map .UnitPrice %))
                })
              (group-by .Category products-list))) )
    (doseq (x categories) (dump-inline x))
  ))
(linq87)"),

                Does.StartWith(@"
{category:Beverages,most-expensive-price:263.5}
{category:Condiments,most-expensive-price:43.9}
{category:Produce,most-expensive-price:53}
{category:Meat/Poultry,most-expensive-price:123.79}
{category:Seafood,most-expensive-price:62.5}
{category:Dairy Products,most-expensive-price:55}
{category:Confections,most-expensive-price:81}
{category:Grains/Cereals,most-expensive-price:38}
".NormalizeNewLines()));
        }
        
        [Test]
        public void linq88()
        {
            Assert.That(render(@"
(defn linq88 []
  (let ( (categories
            (map (fn [g] (
                let ( (max-price (apply max (map .UnitPrice g))) )
                    {
                      :category (.Key g)
                      :most-expensive-products (where #(= (.UnitPrice %) max-price) g) 
                    }))
                (group-by .Category products-list))) )
    (doseq (x categories) (dump-inline x))
  ))
(linq88)"),

                Does.StartWith(@"
{category:Beverages,most-expensive-products:[{Category:Beverages,UnitPrice:263.5,ProductId:38,ProductName:Côte de Blaye,UnitsInStock:17}]}
{category:Condiments,most-expensive-products:[{Category:Condiments,UnitPrice:43.9,ProductId:63,ProductName:Vegie-spread,UnitsInStock:24}]}
{category:Produce,most-expensive-products:[{Category:Produce,UnitPrice:53,ProductId:51,ProductName:Manjimup Dried Apples,UnitsInStock:20}]}
{category:Meat/Poultry,most-expensive-products:[{Category:Meat/Poultry,UnitPrice:123.79,ProductId:29,ProductName:Thüringer Rostbratwurst,UnitsInStock:0}]}
{category:Seafood,most-expensive-products:[{Category:Seafood,UnitPrice:62.5,ProductId:18,ProductName:Carnarvon Tigers,UnitsInStock:42}]}
{category:Dairy Products,most-expensive-products:[{Category:Dairy Products,UnitPrice:55,ProductId:59,ProductName:Raclette Courdavault,UnitsInStock:79}]}
{category:Confections,most-expensive-products:[{Category:Confections,UnitPrice:81,ProductId:20,ProductName:Sir Rodney's Marmalade,UnitsInStock:40}]}
{category:Grains/Cereals,most-expensive-products:[{Category:Grains/Cereals,UnitPrice:38,ProductId:56,ProductName:Gnocchi di nonna Alice,UnitsInStock:21}]}
".NormalizeNewLines()).Or.StartsWith(@"
{category:Beverages,most-expensive-products:[{UnitsInStock:17,ProductName:Côte de Blaye,UnitPrice:263.5,Category:Beverages,ProductId:38}]}
{category:Condiments,most-expensive-products:[{UnitsInStock:24,ProductName:Vegie-spread,UnitPrice:43.9,Category:Condiments,ProductId:63}]}
{category:Produce,most-expensive-products:[{UnitsInStock:20,ProductName:Manjimup Dried Apples,UnitPrice:53,Category:Produce,ProductId:51}]}
{category:Meat/Poultry,most-expensive-products:[{UnitsInStock:0,ProductName:Thüringer Rostbratwurst,UnitPrice:123.79,Category:Meat/Poultry,ProductId:29}]}
{category:Seafood,most-expensive-products:[{UnitsInStock:42,ProductName:Carnarvon Tigers,UnitPrice:62.5,Category:Seafood,ProductId:18}]}
{category:Dairy Products,most-expensive-products:[{UnitsInStock:79,ProductName:Raclette Courdavault,UnitPrice:55,Category:Dairy Products,ProductId:59}]}
{category:Confections,most-expensive-products:[{UnitsInStock:40,ProductName:Sir Rodney's Marmalade,UnitPrice:81,Category:Confections,ProductId:20}]}
{category:Grains/Cereals,most-expensive-products:[{UnitsInStock:21,ProductName:Gnocchi di nonna Alice,UnitPrice:38,Category:Grains/Cereals,ProductId:56}]}
".NormalizeNewLines()).Or.StartsWith(@"
{category:Beverages,most-expensive-products:[{ProductId:38,ProductName:Côte de Blaye,Category:Beverages,UnitPrice:263.5,UnitsInStock:17}]}
{category:Condiments,most-expensive-products:[{ProductId:63,ProductName:Vegie-spread,Category:Condiments,UnitPrice:43.9,UnitsInStock:24}]}
{category:Produce,most-expensive-products:[{ProductId:51,ProductName:Manjimup Dried Apples,Category:Produce,UnitPrice:53,UnitsInStock:20}]}
{category:Meat/Poultry,most-expensive-products:[{ProductId:29,ProductName:Thüringer Rostbratwurst,Category:Meat/Poultry,UnitPrice:123.79,UnitsInStock:0}]}
{category:Seafood,most-expensive-products:[{ProductId:18,ProductName:Carnarvon Tigers,Category:Seafood,UnitPrice:62.5,UnitsInStock:42}]}
{category:Dairy Products,most-expensive-products:[{ProductId:59,ProductName:Raclette Courdavault,Category:Dairy Products,UnitPrice:55,UnitsInStock:79}]}
{category:Confections,most-expensive-products:[{ProductId:20,ProductName:Sir Rodney's Marmalade,Category:Confections,UnitPrice:81,UnitsInStock:40}]}
{category:Grains/Cereals,most-expensive-products:[{ProductId:56,ProductName:Gnocchi di nonna Alice,Category:Grains/Cereals,UnitPrice:38,UnitsInStock:21}]}
".NormalizeNewLines()));
        }
        
        [Test]
        public void linq89()
        {
            Assert.That(render(@"
(defn linq89 []
  (let ( (numbers [5 4 1 3 9 8 6 7 2 0]) 
         (avg) )
    (setq avg (average numbers))
    (println ""The average number is "" avg)
  ))
(linq89)"),

                Does.StartWith(@"
The average number is 4.5
".NormalizeNewLines()));
        }
        
        [Test]
        public void linq90()
        {
            Assert.That(render(@"
(defn linq90 []
  (let ( (words [""cherry"", ""apple"", ""blueberry""])
         (average-length) )
    (setq average-length (apply average (map count words)))
    (println ""The average word length is "" average-length "" characters."")
  ))
(linq90)"),

                Does.StartWith(@"
The average word length is 6.6666666666666
".NormalizeNewLines()));
        }
        
        [Test]
        public void linq91()
        {
            Assert.That(render(@"
(defn linq91 []
  (let ( (categories
             (map #(it {
                  :category (.Key %)
                  :average-price (apply average (map .UnitPrice %))
                })
              (group-by .Category products-list))) )
    (doseq (x categories) (dump-inline x))
  ))
(linq91)")
    .Replace("37.979166666666664","37.9791666666667") //.NET Core 3.1
    .Replace("54.00666666666667","54.0066666666667"),

                Does.StartWith(@"
{category:Beverages,average-price:37.9791666666667}
{category:Condiments,average-price:23.0625}
{category:Produce,average-price:32.37}
{category:Meat/Poultry,average-price:54.0066666666667}
{category:Seafood,average-price:20.6825}
{category:Dairy Products,average-price:28.73}
{category:Confections,average-price:25.16}
{category:Grains/Cereals,average-price:20.25}
".NormalizeNewLines()));
        }

        [Test]
        public void linq92()
        {
            Assert.That(render(@"
(defn linq92 []
  (let ( (dbls [1.7 2.3 1.9 4.1 2.9]) 
         (product) )
    (setq product (reduce * dbls))
    (println ""Total product of all numbers: "" product)
  ))
(linq92)"), 
                
                Does.StartWith(@"
Total product of all numbers: 88.3308".NormalizeNewLines()));
        }
        
        [Test]
        public void linq93()
        {
            Assert.That(render(@"
(defn linq93 []
  (let ( (start-balance 100)
         (attempted-withdrawls [20 10 40 50 10 70 30])
         (end-balance) )
    (setq end-balance (reduce (fn [balance withdrawl] (if (> balance withdrawl) (- balance withdrawl) balance)) 
                       attempted-withdrawls start-balance))
    (println ""Ending balance: "" end-balance)
  ))
(linq93)"), 
                
                Does.StartWith(@"
Ending balance: 20
".NormalizeNewLines()));
        }
        
        [Test]
        public void linq94()
        {
            Assert.That(render(@"
(defn linq94 []
  (let ( (numbers-a [0 2 4 5 6 8 9])
         (numbers-b [1 3 5 7 8]) )
    (setq all-numbers (flatten [numbers-a numbers-b]))
    (println ""All numbers from both arrays:"")
    (doseq (n all-numbers) (println n))
  ))
(linq94)"), 
                
                Does.StartWith(@"
All numbers from both arrays:
0
2
4
5
6
8
9
1
3
5
7
8
".NormalizeNewLines()));
        }
        
        [Test]
        public void linq95()
        {
            Assert.That(render(@"
(defn linq95 []
  (let ( (customer-names (map .CompanyName customers-list))
         (product-names  (map .ProductName products-list))
         (all-names) )
    (setq all-names (flatten [customer-names product-names]))
    (println ""Customer and product names:"")
    (doseq (x all-names) (println x))
  ))
(linq95)"), 
                
                Does.StartWith(@"
Customer and product names:
Alfreds Futterkiste
Ana Trujillo Emparedados y helados
Antonio Moreno Taquería
Around the Horn
Berglunds snabbköp
Blauer See Delikatessen
".NormalizeNewLines()));
        }
        
        [Test]
        public void linq96()
        {
            Assert.That(render(@"
(defn linq96 []
  (let ( (words-a [""cherry"" ""apple"" ""blueberry""]) 
         (words-b [""cherry"" ""apple"" ""blueberry""]) )
        
    (setq match (/sequenceEquals words-a words-b))
    (println ""The sequences match: "" match)
  ))
(linq96)"), 
                
                Does.StartWith(@"
The sequences match: True
".NormalizeNewLines()));
        }
        
        [Test]
        public void linq97()
        {
            Assert.That(render(@"
(defn linq97 []
  (let ( (words-a [""cherry"" ""apple"" ""blueberry""]) 
         (words-b [""apple"" ""blueberry"" ""cherry""]) )
        
    (setq match (/sequenceEquals words-a words-b))
    (println ""The sequences match: "" match)
  ))
(linq97)"), 
                
                Does.StartWith(@"
The sequences match: nil
".NormalizeNewLines()));
        }
        
        [Test]
        public void linq99()
        {
            Assert.That(render(@"
(defn linq99 []
  (let ( (numbers [5 4 1 3 9 8 6 7 2 0])
         (i 0) )
    (setq q (map #(it (fn [] (f++ i))) numbers))
    (doseq (v q) (println ""v = "" (v) "", i = "" i))
  ))
(linq99)"), 
                
                Does.StartWith(@"
v = 0, i = 1
v = 1, i = 2
v = 2, i = 3
v = 3, i = 4
v = 4, i = 5
v = 5, i = 6
v = 6, i = 7
v = 7, i = 8
v = 8, i = 9
v = 9, i = 10
".NormalizeNewLines()));
        }
        
        [Test]
        public void linq100()
        {
            Assert.That(render(@"
(defn linq100 []
  (let ( (numbers [5 4 1 3 9 8 6 7 2 0])
         (i 0) )
    (setq q (map #(it (f++ i)) numbers))
    (doseq (v q) (println ""v = "" v "", i = "" i))
  ))
(linq100)"), 
                
                Does.StartWith(@"
v = 0, i = 10
v = 1, i = 10
v = 2, i = 10
v = 3, i = 10
v = 4, i = 10
v = 5, i = 10
v = 6, i = 10
v = 7, i = 10
v = 8, i = 10
v = 9, i = 10
".NormalizeNewLines()));
        }
                
        [Test]
        public void linq101()
        {
            Assert.That(render(@"
(defn linq101 []
  (let ( (numbers [5 4 1 3 9 8 6 7 2 0]) )
 
    (defn low-numbers []
      (where #(<= % 3) numbers))

    (println ""First run numbers <= 3:"")
    (doseq (n (low-numbers)) (println n))

    (setq numbers (map #(- %) numbers))
    
    (println ""Second run numbers <= 3"")
    (doseq (n (low-numbers)) (println n))
  ))
(linq101)"), 
                
                Does.StartWith(@"
First run numbers <= 3:
1
3
2
0
Second run numbers <= 3
-5
-4
-1
-3
-9
-8
-6
-7
-2
0
".NormalizeNewLines()));
        }
        
        [Test]
        public void test()
        {
            print("(where #(<= % 3) [-5 -4 1])");
            print(@"(setq numbers [5 4 1 3 9 8 6 7 2 0])
    (defn low-numbers []
      (where #(<= % 3) (map #(- %) numbers)))
    (low-numbers)");
            
//            print(@"(setq numbers '(5 4 1 3 9 8 6 7 2 0)) (take-while (fn (c) (>= (1st c) (2nd c))) (mapcar-index cons numbers))");

//            print("(setq numbers-a '(1 2 3)) (setq numbers-b '(3 4 5)) (zip (fn (a b) { :a a :b b }) numbers-a numbers-b)");
//            print("(map #(* 2 %) (range 10))");
//            print("(fn (x) (.ProductName x))");
//            print(@"(fn (x) (new-map (list ""ProductName"" (.ProductName x)) ))");
        }
        
    }
}
