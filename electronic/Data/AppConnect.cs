using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace electronic.Data
{
    internal class AppConnect
    {
        public static electronicEntities model1;
        public static Frame mainFrame;
        public static User user;//пользователь

    }
    public partial class User
    {
        public string FIO => $"{surname} {name} {patronymic}";
    }
}
