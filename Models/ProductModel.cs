using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleMES.Models
{
    public class ProductModel
    {
        public string ProductCode { get; set; }
        public string ProductName { get; set; }
        public double SetTemperature { get; set; }
        public double SetPressure { get; set; }
        public string Description { get; set; }
    }
}
