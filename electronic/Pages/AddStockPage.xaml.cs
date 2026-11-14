using electronic.Data;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace electronic.Pages
{
    public partial class AddStockPage : Page
    {
        private Product _currentProduct;

        public AddStockPage(Product product)
        {
            InitializeComponent();
            _currentProduct = product;

            txtTitle.Text = $"Приход: {_currentProduct.name}";

            // Загружаем все варианты памяти из справочника Size
            var sizes = AppConnect.model1.Size.ToList();
            cmbSize.ItemsSource = sizes;

            if (sizes.Count > 0)
                cmbSize.SelectedIndex = 0; // Выбираем первый по умолчанию
        }

        private void btnSaveStock_Click(object sender, RoutedEventArgs e)
        {
            // Проверяем, что ввели целое число больше нуля
            if (!int.TryParse(txtCount.Text, out int addedCount) || addedCount <= 0 || addedCount > 100000)
            {
                MessageBox.Show("Пожалуйста, введите корректное количество (целое число больше 0 и меньше 100000).", "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            electronic.Data.Size selectedSize = cmbSize.SelectedItem as electronic.Data.Size;
            int sizeId = selectedSize.id;

            try
            {
                // Ищем, есть ли уже этот товар с ТАКОЙ ЖЕ памятью на складе
                var existingStock = AppConnect.model1.Stock.FirstOrDefault(s => s.idProduct == _currentProduct.id && s.idSize == sizeId);

                if (existingStock != null)
                {
                    // Если есть — просто плюсуем остатки
                    existingStock.count += addedCount;
                }
                else
                {
                    // Если это первая поставка такого объема памяти — создаем новую запись на складе
                    Stock newStock = new Stock
                    {
                        idProduct = _currentProduct.id,
                        idSize = sizeId,
                        count = addedCount
                    };
                    AppConnect.model1.Stock.Add(newStock);
                }

                AppConnect.model1.SaveChanges();
                MessageBox.Show($"Успешно добавлено {addedCount} шт. на склад!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                NavigationService.GoBack();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка сохранения в базу: " + ex.Message);
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }
    }
}