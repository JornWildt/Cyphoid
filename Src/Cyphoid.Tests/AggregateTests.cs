using Cyphoid.Core.Expressions;

namespace Cyphoid.Tests
{
  [TestFixture]
  internal class AggregateTests : TestHelper
  {
    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
      // Salespeople
      Graph.AddNode("Alice", "salesperson");
      Graph.AddNode("Bob", "salesperson");
      Graph.AddNode("Clara", "salesperson");

      Graph.SetNodeProperty("Alice", "name", "Alice Andersen");
      Graph.SetNodeProperty("Bob", "name", "Bob Berg");
      Graph.SetNodeProperty("Clara", "name", "Clara Christiansen");

      // Customers
      Graph.AddNode("Acme", "customer");
      Graph.AddNode("Globex", "customer");
      Graph.AddNode("Initech", "customer");

      Graph.SetNodeProperty("Acme", "name", "Acme Corp");
      Graph.SetNodeProperty("Globex", "name", "Globex Ltd");
      Graph.SetNodeProperty("Initech", "name", "Initech ApS");

      // Products
      Graph.AddNode("Laptop", "product");
      Graph.AddNode("Mouse", "product");
      Graph.AddNode("Monitor", "product");
      Graph.AddNode("Keyboard", "product");

      Graph.SetNodeProperty("Laptop", "name", "Laptop");
      Graph.SetNodeProperty("Laptop", "category", "Hardware");
      Graph.SetNodeProperty("Laptop", "listPrice", 1200.0);

      Graph.SetNodeProperty("Mouse", "name", "Mouse");
      Graph.SetNodeProperty("Mouse", "category", "Accessory");
      Graph.SetNodeProperty("Mouse", "listPrice", 25.0);

      Graph.SetNodeProperty("Monitor", "name", "Monitor");
      Graph.SetNodeProperty("Monitor", "category", "Hardware");
      Graph.SetNodeProperty("Monitor", "listPrice", 300.0);

      Graph.SetNodeProperty("Keyboard", "name", "Keyboard");
      Graph.SetNodeProperty("Keyboard", "category", "Accessory");
      Graph.SetNodeProperty("Keyboard", "listPrice", 80.0);

      // Orders
      Graph.AddNode("Order1", "order");
      Graph.AddNode("Order2", "order");
      Graph.AddNode("Order3", "order");
      Graph.AddNode("Order4", "order");
      Graph.AddNode("Order5", "order");
      Graph.AddNode("Order6", "order");

      Graph.SetNodeProperty("Order1", "orderNo", "SO-001");
      Graph.SetNodeProperty("Order1", "orderDate", "2026-03-01");
      Graph.SetNodeProperty("Order1", "discountPct", 0.10);

      Graph.SetNodeProperty("Order2", "orderNo", "SO-002");
      Graph.SetNodeProperty("Order2", "orderDate", "2026-03-02");
      Graph.SetNodeProperty("Order2", "discountPct", 0.00);

      Graph.SetNodeProperty("Order3", "orderNo", "SO-003");
      Graph.SetNodeProperty("Order3", "orderDate", "2026-03-03");
      Graph.SetNodeProperty("Order3", "discountPct", 0.05);

      Graph.SetNodeProperty("Order4", "orderNo", "SO-004");
      Graph.SetNodeProperty("Order4", "orderDate", "2026-03-03");
      Graph.SetNodeProperty("Order4", "discountPct", 0.15);

      Graph.SetNodeProperty("Order5", "orderNo", "SO-005");
      Graph.SetNodeProperty("Order5", "orderDate", "2026-03-04");
      Graph.SetNodeProperty("Order5", "discountPct", 0.00);

      Graph.SetNodeProperty("Order6", "orderNo", "SO-006");
      Graph.SetNodeProperty("Order6", "orderDate", "2026-03-05");
      Graph.SetNodeProperty("Order6", "discountPct", 0.20);

      // Who sold what order
      Graph.AddEdge("Alice", "Order1", "sold");
      Graph.AddEdge("Alice", "Order2", "sold");
      Graph.AddEdge("Bob", "Order3", "sold");
      Graph.AddEdge("Bob", "Order4", "sold");
      Graph.AddEdge("Clara", "Order5", "sold");
      Graph.AddEdge("Clara", "Order6", "sold");

      // Which customer placed the order
      Graph.AddEdge("Order1", "Acme", "for_customer");
      Graph.AddEdge("Order2", "Globex", "for_customer");
      Graph.AddEdge("Order3", "Acme", "for_customer");
      Graph.AddEdge("Order4", "Initech", "for_customer");
      Graph.AddEdge("Order5", "Globex", "for_customer");
      Graph.AddEdge("Order6", "Acme", "for_customer");

      // Order lines as nodes, so you can aggregate quantities / amounts cleanly
      Graph.AddNode("Line1", "order_line");
      Graph.AddNode("Line2", "order_line");
      Graph.AddNode("Line3", "order_line");
      Graph.AddNode("Line4", "order_line");
      Graph.AddNode("Line5", "order_line");
      Graph.AddNode("Line6", "order_line");
      Graph.AddNode("Line7", "order_line");
      Graph.AddNode("Line8", "order_line");
      Graph.AddNode("Line9", "order_line");
      Graph.AddNode("Line10", "order_line");

      // Order 1
      Graph.SetNodeProperty("Line1", "quantity", 2);
      Graph.SetNodeProperty("Line1", "unitPrice", 1100.0);
      Graph.SetNodeProperty("Line1", "lineAmount", 2200.0);
      Graph.AddEdge("Order1", "Line1", "has_line");
      Graph.AddEdge("Line1", "Laptop", "for_product");

      Graph.SetNodeProperty("Line2", "quantity", 5);
      Graph.SetNodeProperty("Line2", "unitPrice", 20.0);
      Graph.SetNodeProperty("Line2", "lineAmount", 100.0);
      Graph.AddEdge("Order1", "Line2", "has_line");
      Graph.AddEdge("Line2", "Mouse", "for_product");

      // Order 2
      Graph.SetNodeProperty("Line3", "quantity", 3);
      Graph.SetNodeProperty("Line3", "unitPrice", 300.0);
      Graph.SetNodeProperty("Line3", "lineAmount", 900.0);
      Graph.AddEdge("Order2", "Line3", "has_line");
      Graph.AddEdge("Line3", "Monitor", "for_product");

      // Order 3
      Graph.SetNodeProperty("Line4", "quantity", 1);
      Graph.SetNodeProperty("Line4", "unitPrice", 1200.0);
      Graph.SetNodeProperty("Line4", "lineAmount", 1200.0);
      Graph.AddEdge("Order3", "Line4", "has_line");
      Graph.AddEdge("Line4", "Laptop", "for_product");

      Graph.SetNodeProperty("Line5", "quantity", 2);
      Graph.SetNodeProperty("Line5", "unitPrice", 75.0);
      Graph.SetNodeProperty("Line5", "lineAmount", 150.0);
      Graph.AddEdge("Order3", "Line5", "has_line");
      Graph.AddEdge("Line5", "Keyboard", "for_product");

      // Order 4
      Graph.SetNodeProperty("Line6", "quantity", 10);
      Graph.SetNodeProperty("Line6", "unitPrice", 22.0);
      Graph.SetNodeProperty("Line6", "lineAmount", 220.0);
      Graph.AddEdge("Order4", "Line6", "has_line");
      Graph.AddEdge("Line6", "Mouse", "for_product");

      Graph.SetNodeProperty("Line7", "quantity", 4);
      Graph.SetNodeProperty("Line7", "unitPrice", 280.0);
      Graph.SetNodeProperty("Line7", "lineAmount", 1120.0);
      Graph.AddEdge("Order4", "Line7", "has_line");
      Graph.AddEdge("Line7", "Monitor", "for_product");

      // Order 5
      Graph.SetNodeProperty("Line8", "quantity", 6);
      Graph.SetNodeProperty("Line8", "unitPrice", 70.0);
      Graph.SetNodeProperty("Line8", "lineAmount", 420.0);
      Graph.AddEdge("Order5", "Line8", "has_line");
      Graph.AddEdge("Line8", "Keyboard", "for_product");

      // Order 6
      Graph.SetNodeProperty("Line9", "quantity", 1);
      Graph.SetNodeProperty("Line9", "unitPrice", 1000.0);
      Graph.SetNodeProperty("Line9", "lineAmount", 1000.0);
      Graph.AddEdge("Order6", "Line9", "has_line");
      Graph.AddEdge("Line9", "Laptop", "for_product");

      Graph.SetNodeProperty("Line10", "quantity", 2);
      Graph.SetNodeProperty("Line10", "unitPrice", 250.0);
      Graph.SetNodeProperty("Line10", "lineAmount", 500.0);
      Graph.AddEdge("Order6", "Line10", "has_line");
      Graph.AddEdge("Line10", "Monitor", "for_product");
    }


    [TestCase("MATCH (o:order) RETURN COUNT(*) AS value", 6)]
    [TestCase("MATCH (o:order)-[:has_line]->(l:order_line) RETURN sum(l.lineAmount) AS value", 7810.0)]
    [TestCase("MATCH (o:order)-[:has_line]->(l:order_line) RETURN min(l.lineAmount) AS value", 150.0)]
    [TestCase("MATCH (o:order)-[:has_line]->(l:order_line) RETURN max(l.lineAmount) AS value", 2200.0)]
    public async Task ItCanExecuteBasicQuery(string input, object expectedValue)
    {
      // Act
      var result = await ExecuteQuery(input);

      // Assert
      Assert.That(result.Print, Is.EqualTo(input.Replace("'", "\"")));

      Assert.That(result.Rows.Count, Is.EqualTo(1));
      Assert.That(result.Rows[0]["value"], Is.EqualTo(MixedValue.FromObject(expectedValue)));
    }
  }
}
