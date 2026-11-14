using electronic.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;

namespace electronic.Pages
{
    /// <summary>
    /// Логика взаимодействия для ProductPage.xaml
    /// </summary>
    public partial class ProductPage : Page
    {
        // храню тут все товары, чтобы поиск работал быстро и без лишних запросов к бд
        private List<Product> _all = new List<Product>();
        private MainWindow mw = Application.Current.MainWindow as MainWindow;

        public ProductPage()
        {
            InitializeComponent();
            IsVisibleChanged += Page_IsVisibleChanged;

            // вывожу фио пользователя или пишу гость, если не авторизовались
            
            mw.FIO.Text = AppConnect.user?.FIO ?? "Гость";
            if (AppConnect.user != null)
            {
                mw.btnProfile.Visibility = Visibility.Visible; // Показываем профиль
            }
            else
            {
                mw.btnProfile.Visibility = Visibility.Collapsed; // Прячем от гостя
            }
            // прячу лишние кнопки в зависимости от прав
            CheckRoles();
            Load();
        }
        private void Page_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (Visibility == Visibility.Visible)
            {
                // сначала очищаем, чтобы привязки (в том числе ImagePath) пересчитались
                listProduct.ItemsSource = null;
                listProduct.ItemsSource = AppConnect.model1.Product.ToList();
            }
        }
        private void CheckRoles()
        {
            btnAdd.Visibility = Visibility.Collapsed;
            btnDelete.Visibility = Visibility.Collapsed;
            btnOrders.Visibility = Visibility.Collapsed;

            // Прячем кнопку пользователей ото всех по умолчанию
            btnUsers.Visibility = Visibility.Collapsed;

            if (AppConnect.user != null)
            {
                // Кнопку "Заказы" видят все авторизованные
                btnOrders.Visibility = Visibility.Visible;

                // МЕНЕДЖЕР
                if (AppConnect.user.idRole == 2)
                {
                    btnAdd.Visibility = Visibility.Visible;
                    btnDelete.Visibility = Visibility.Collapsed;
                    // Пользователей менеджер НЕ видит
                }
                // АДМИНИСТРАТОР
                else if (AppConnect.user.idRole == 1)
                {
                    btnAdd.Visibility = Visibility.Visible;
                    btnDelete.Visibility = Visibility.Visible;
                    btnUsers.Visibility = Visibility.Visible;
                }
            }
        }

        private void Load()
        {
            // скачиваю всю таблицу товаров разом
            _all = AppConnect.model1.Product.ToList();

            // заполняю сортировку
            cmbSort.Items.Clear();
            cmbSort.Items.Add("Без сортировки");
            cmbSort.Items.Add("Сначала дешевые");
            cmbSort.Items.Add("Сначала дорогие");
            cmbSort.SelectedIndex = 0;

            // заполняю фильтр категорий простым текстом
            cmbFilter.Items.Clear();
            cmbFilter.Items.Add("Все категории");
            foreach (var c in AppConnect.model1.Category.ToList())
            {
                // обращаюсь к свойству category1
                cmbFilter.Items.Add(c.category1);
            }
            cmbFilter.SelectedIndex = 0;

            Apply();
        }
        // срабатывает, когда выбираем категорию или сортировку
        private void Filters_Changed(object sender, SelectionChangedEventArgs e)
        {
            Apply();
        }

        // срабатывает, когда печатаем текст в поиске
        private void Filters_Changed(object sender, TextChangedEventArgs e)
        {
            Apply();
        }

        // главный метод, который собирает все фильтры в кучу
        private void Apply()
        {
            var list = _all.AsEnumerable();

            // ищем по тексту (название, описание и производитель)
            string q = txtSearch.Text.Trim().ToLower();
            if (q.Length > 0)
            {
                list = list.Where(p =>
                    // ищем в названии
                    (p.name != null && p.name.ToLower().Contains(q)) ||

                    // ищем в описании (обязательно проверяем на null, чтобы прога не вылетела, если описание пустое)
                    (p.description != null && p.description.ToLower().Contains(q)) ||

                    // ищем по составу (если у тебя есть такое поле, если нет можно удалить эту строчку)
                    (p.consist != null && p.consist.ToLower().Contains(q)) ||

                    // крутая фича: ищем еще и по производителю!
                    (p.Manufacturer != null && p.Manufacturer.manufacturer1.ToLower().Contains(q))
                );
            }

            // проверяем категорию
            if (cmbFilter.SelectedIndex > 0)
            {
                string cat = cmbFilter.SelectedItem.ToString();
                list = list.Where(p => p.Category.category1 == cat);
            }

            // сортируем по цене
            if (cmbSort.SelectedIndex == 1)
                list = list.OrderBy(p => p.price);
            if (cmbSort.SelectedIndex == 2)
                list = list.OrderByDescending(p => p.price);

            // выводим результат на экран
            listProduct.ItemsSource = list.ToList();
        }

        // если дернули любой комбобокс или ввели текст - просто пересчитываем список
        private void Filters_Changed(object sender, EventArgs e) => Apply();

        private void listProduct_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // проверяю, что точно кликнули по товару, а не по пустому месту
            if (!(listProduct.SelectedItem is Product p)) return;

            // редактировать могут только админы и менеджеры
            if (AppConnect.user != null && (AppConnect.user.idRole == 1 || AppConnect.user.idRole == 2))
            {
                NavigationService.Navigate(new AddEditProductPage(p));
            }
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            // передаю null, так как товар создается с нуля
            NavigationService.Navigate(new AddEditProductPage(null));
        }

        private void Del_Click(object sender, RoutedEventArgs e)
        {
            // Проверка прав
            if (AppConnect.user?.idRole != 1)
            {
                MessageBox.Show("Удаление доступно только администратору.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!(listProduct.SelectedItem is Product p)) return;

            if (MessageBox.Show($"Точно удалить товар: {p.name}?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            try
            {
                // Проверяем, есть ли запись в OrderStock, которая ссылается на этот товар через склад (Stock)
                bool isInOrders = AppConnect.model1.Stock.Any(s => s.idProduct == p.id && AppConnect.model1.OrderStock.Any(os => os.idStock == s.id));

                if (isInOrders)
                {
                    // Строгое сообщение по требованиям
                    MessageBox.Show("Невозможно удалить товар, так как он присутствует в одном или нескольких заказах.", "Ошибка удаления", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                //Если в заказах товара нет - удаляем его со склада и из каталога
                var stockItems = AppConnect.model1.Stock.Where(s => s.idProduct == p.id).ToList();
                foreach (var stock in stockItems)
                {
                    AppConnect.model1.Stock.Remove(stock);
                }

                AppConnect.model1.Product.Remove(p);
                AppConnect.model1.SaveChanges();

                MessageBox.Show("Товар успешно удален!");
                Load(); // Обновляем список
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка базы данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cart_Click(object sender, RoutedEventArgs e)
        {
            if (AppConnect.user == null)
            {
                MessageBox.Show("Сначала нужно авторизоваться!");
                return;
            }
            NavigationService.Navigate(new CartPage());
        }

        private void Orders_Click(object sender, RoutedEventArgs e)
        {
            if (AppConnect.user == null)
            {
                MessageBox.Show("Сначала нужно авторизоваться!");
                return;
            }
            NavigationService.Navigate(new OrderPage());
        }
        // Скрытие кнопки "В корзину" внутри каждого товара для админа и менеджера
        private void btnAddToCart_Loaded(object sender, RoutedEventArgs e)
        {
         
        }

        // Добавление конкретного товара в корзину
        private void btnAddToCart_Click(object sender, RoutedEventArgs e)
        {
            Button btn = (Button)sender;
            Product selectedProduct = (Product)btn.DataContext;

            if (AppConnect.user == null)
            {
                MessageBox.Show("Сначала нужно авторизоваться!");
                return;
            }

            try
            {
                // Ищем товар в таблице Stock (Склад), чтобы получить его idStock
                var stockItem = AppConnect.model1.Stock.FirstOrDefault(s => s.idProduct == selectedProduct.id);
                if (stockItem == null)
                {
                    MessageBox.Show("Этого товара пока нет на складе!");
                    return;
                }

                // Проверяем, есть ли этот товар уже в корзине
                var existingItem = AppConnect.model1.CartItems.FirstOrDefault(c => c.idUser == AppConnect.user.id && c.idStock == stockItem.id);

                if (existingItem != null)
                {
                    // Если товар уже есть, просто увеличиваем количество
                    existingItem.count += 1;
                }
                else
                {
                    // Если нет, создаем новую запись в БД
                    CartItems newItem = new CartItems();
                    newItem.idUser = AppConnect.user.id;
                    newItem.idStock = stockItem.id;
                    newItem.count = 1;

                    AppConnect.model1.CartItems.Add(newItem);
                }

                // Сохраняем в базу данных
                AppConnect.model1.SaveChanges();
                MessageBox.Show($"Товар '{selectedProduct.name}' сохранен в вашу корзину!");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка: " + ex.Message);
            }
        }
        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            // сбрасываю пользователя и выхожу назад
            AppConnect.user = null;
            mw.btnProfile.Visibility = Visibility.Collapsed;
            NavigationService.GoBack();
        }

        private void btnAddStock_Click(object sender, RoutedEventArgs e)
        {
            if (AppConnect.user == null || AppConnect.user.idRole == 3)
            {
                MessageBox.Show("У вас нет прав для пополнения склада.");
                return;
            }

            // Получаем товар из кнопки
            var btn = sender as Button;
            var selectedProduct = btn.DataContext as Product;

            if (selectedProduct != null)
            {
                // Переходим на страницу добавления остатков
                NavigationService.Navigate(new AddStockPage(selectedProduct));
            }
        }

        private void btnUsers_Click(object sender, RoutedEventArgs e)
        {
            if (AppConnect.user == null || AppConnect.user.idRole != 1)
            {
                MessageBox.Show("У вас нет прав доступа к этой странице!");
                return;
            }

            // Переходим на нашу новую страницу!
            NavigationService.Navigate(new UsersPage());
        }
    }
}