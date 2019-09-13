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
        (orders (flatmap (fn (c) 
                    (flatmap (fn (o) 
                        (if (> (.OrderDate o) (DateTime. 1998 1 1) )
                        {
                            :customer-id (.CustomerId c) 
                            :order-id    (.OrderId o) 
                            :order-date  (.OrderDate o)
                        })) (.Orders c) )
                ) customers-list)  ))
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
(defn linq17 ()
  (let ( 
        (orders (flatmap (fn (c) 
                    (flatmap (fn (o) 
                        (if (>= (:total o) 2000)
                        {
                            :customer-id (.CustomerId c) 
                            :order-id    (.OrderId o) 
                            :total       (.Total o)
                        })) (.Orders c) )
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
(defn linq18 ()
  (let ( (cutoff-date (DateTime. 1997 1 1))
         (orders) )
    (setq orders (flatmap (fn (c) 
          (flatmap (fn (o) 
              (if (>= (.OrderDate o) cutoff-date)
              {
                  :customer-id (.CustomerId c) 
                  :order-id    (.OrderId o) 
              })) (.Orders c) )
      ) (filter #(= (.Region %) ""WA"") customers-list) ) )
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
(defn linq19 ()
  (let ( (customer-orders 
            (map 
                (fn (x) (str ""Customer #"" (:i x) "" has an order with OrderID "" (.OrderId (:o x)))) 
                (/flatten (map-index (fn (c i) (map (fn (o) { :o o :i (1+ i) }) (.Orders c))) customers-list)) 
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
(defn linq20 ()
  (let ( (numbers '(5 4 1 3 9 8 6 7 2 0))
        (first-3-numbers)) 
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
(defn linq21 ()
  (let ( (first-3-wa-orders) )
    (setq first-3-wa-orders 
      (take 3 
        (flatmap (fn (c) 
          (flatmap (fn (o) 
              {
                  :customer-id (.CustomerId c) 
                  :order-id    (.OrderId o) 
                  :order-date  (.OrderDate o)
              }) (.Orders c) )
        ) (filter #(= (.Region %) ""WA"") customers-list) )) )
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
(defn linq22 ()
  (let ( (numbers '(5 4 1 3 9 8 6 7 2 0)) 
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
(defn linq23 ()
  (let ( (all-but-first-2-orders
      (skip 2 
        (flatmap (fn (c) 
          (flatmap (fn (o) 
              {
                  :customer-id (.CustomerId c) 
                  :order-id    (.OrderId o) 
                  :order-date  (.OrderDate o)
              }) (.Orders c) )
        ) (filter #(= (.Region %) ""WA"") customers-list) )) ))
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
(defn linq24 ()
  (let ( (numbers '(5 4 1 3 9 8 6 7 2 0))
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
(defn linq25 ()
  (let ( (numbers '(5 4 1 3 9 8 6 7 2 0) )
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
(defn linq26 ()
  (let ( (numbers '(5 4 1 3 9 8 6 7 2 0))
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
(defn linq27 ()
  (let ( (numbers '(5 4 1 3 9 8 6 7 2 0))
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
(defn linq28 ()
  (let ( (words '(""cherry"" ""apple"" ""blueberry""))
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
(defn linq29 ()
  (let ( (words '(""cherry"" ""apple"" ""blueberry""))
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
(defn linq30 ()
  (let ( (sorted-products (sort-by .ProductName products-list)) )
    (doseq (p sorted-products) (dump-inline p))
  ))
(linq30)"), 
                
                Does.StartWith(@"
{UnitsInStock:0,ProductName:Alice Mutton,UnitPrice:39,Category:Meat/Poultry,ProductId:17}
{UnitsInStock:13,ProductName:Aniseed Syrup,UnitPrice:10,Category:Condiments,ProductId:3}
{UnitsInStock:123,ProductName:Boston Crab Meat,UnitPrice:18.4,Category:Seafood,ProductId:40}
{UnitsInStock:19,ProductName:Camembert Pierrot,UnitPrice:34,Category:Dairy Products,ProductId:60}
{UnitsInStock:42,ProductName:Carnarvon Tigers,UnitPrice:62.5,Category:Seafood,ProductId:18}
".NormalizeNewLines()));
        }

        [Test]
        public void test()
        {
//            print("(setq i 0)(setq numbers '(5 4 1 3 9 8 6 7 2 0)) (skip-while #(>= % (incf+ i)) numbers)");
//            print(@"(setq numbers '(5 4 1 3 9 8 6 7 2 0)) (take-while (fn (c) (>= (1st c) (2nd c))) (mapcar-index cons numbers))");

//            print("(setq numbers-a '(1 2 3)) (setq numbers-b '(3 4 5)) (zip (fn (a b) { :a a :b b }) numbers-a numbers-b)");
//            print("(map #(* 2 %) (range 10))");
//            print("(fn (x) (.ProductName x))");
//            print(@"(fn (x) (new-map (list ""ProductName"" (.ProductName x)) ))");
        }
        
    }
}
