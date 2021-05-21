
using ErpNet.Api.Client.DomainApi.Crm.Sales;
using System;
using System.Threading.Tasks;
using Xunit;

namespace ErpNet.Api.Client.DomainApi.Test
{
    public class UnitTest1
    {
        private DomainApiService CreateService() => new DomainApiService("https://fake.com", () => Task.FromResult("access_token"));

        [Fact]
        public void ExpandTest1()
        {
            DomainApiService cnn = CreateService();

            var cmd = cnn.Command<SalesOrder>()
                .Expand(o => o.Lines.ExpandItems(l => l.Product.Expand(p => p.ProductGroup)));

            Assert.Equal("Lines($expand=Product($expand=ProductGroup))", cmd.ExpandClause);
        }

        [Fact]
        public void ExpandTest2()
        {
            DomainApiService cnn = CreateService();

            var cmd = cnn.Command<SalesOrder>()
                .Expand(so => so.Lines
                    .ExpandItems(l => l.Product.Expand(p => p.ProductGroup.ExpandCollection(g => g.RangeProperties)).Expand(p => p.ProductType))
                    .ExpandItems(l => l.Lot)
                    .ExpandItems(l => l.LineStore));

            Assert.Equal("Lines($expand=Product($expand=ProductGroup($expand=RangeProperties),ProductType),Lot,LineStore)", cmd.ExpandClause);
        }

        [Fact]
        public void ExpandTest3()
        {
            DomainApiService cnn = CreateService();

            var cmd = cnn.Command<SalesOrder>()
                .Expand(o => o.Expand(so => so.Customer).ExpandCollection(so => so.Lines));

            Assert.Equal("Customer,Lines", cmd.ExpandClause);
        }

        [Fact]
        public void ExpandTest4()
        {
            var str = "Lines($expand=Product($expand=ProductGroup($expand=RangeProperties),ProductType),Lot,LineStore)";
            var node = ExpandNode.Parse(str, null);
            Assert.Equal(str, node.ExpandClause);


            str = "Lines($expand=Product($select=ProductGroup,ProductType),Lot,LineStore)";
            node = ExpandNode.Parse(str, null);
            Assert.Equal(str, node.ExpandClause);
        }

        [Fact]
        public void ExpandTest5()
        {
            var node = ExpandNode.Parse("Lines", null);
            Assert.Equal("Lines", node.ExpandClause);
        }
    }
}
