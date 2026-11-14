using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace electronic.Data
{
    internal class AppConnect
    {
        //public static electronicEntities model1;
        public static List<Product> Cart = new List<Product>();
        public static electronicEntities model1;
        public static Frame mainFrame;
        public static User user;//пользователь

    }

    public partial class User
    {
        public string FIO => $"{surname} {name} {patronymic}";
    }
    public partial class Product
    {
        public bool IsExpensive => price > 1000;

        public bool HasDiscount => discountPercent.HasValue && discountPercent.Value > 0;
        public string ImagePath
        {
            get
            {
                string fileName = string.IsNullOrEmpty(image) ? "picture.png" : image;
                string fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Image", fileName);

                // Новые картинки (добавленные через программу) лежат на диске
                if (File.Exists(fullPath))
                    return fullPath;

                // Старые картинки вшиты в программу, как было раньше
                return "/Image/" + fileName;
            }
        }
    }
}
