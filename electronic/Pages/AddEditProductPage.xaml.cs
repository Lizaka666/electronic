using electronic.Data;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Win32;
using System.IO;
using System.Windows.Media.Imaging;

namespace electronic.Pages
{
    public partial class AddEditProductPage : Page
    {
        Product _currentProduct = new Product();
        string _selectedFilePath = null;

        public AddEditProductPage(Product selectedProduct)
        {
            InitializeComponent();

            if (selectedProduct != null)
            {
                _currentProduct = selectedProduct;
            }

            DataContext = _currentProduct;

            // Загружаем списки
            cmbCategory.ItemsSource = AppConnect.model1.Category.ToList();
            cmbManufacturer.ItemsSource = AppConnect.model1.Manufacturer.ToList();

            if (selectedProduct != null)
            {
                cmbCategory.SelectedItem = AppConnect.model1.Category.FirstOrDefault(c => c.id == _currentProduct.idCategory);
                cmbManufacturer.SelectedItem = AppConnect.model1.Manufacturer.FirstOrDefault(m => m.id == _currentProduct.idManufacturer);
            }
        }

        // Кнопка "Выбрать изображение"
        private void SelectImage_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "Изображения|*.jpg;*.jpeg;*.png";

            if (dialog.ShowDialog() == true)
            {
                _selectedFilePath = dialog.FileName; // запоминаем, копировать будем при сохранении

                // показываем превью выбранной картинки
                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.UriSource = new Uri(_selectedFilePath);
                bitmap.EndInit();
                imgProduct.Source = bitmap;
            }
        }
        private void Calculate_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Проверяем, что окно уже загрузилось и поля не пустые (защита от вылета программы)
            if (txtPrice == null || txtDiscount == null || txtFinalPrice == null)
                return;

            // Если в поле цены вписали буквы или там пусто, базовая цена будет 0
            decimal.TryParse(txtPrice.Text, out decimal basePrice);

            // Если в поле скидки пусто, скидка будет 0
            decimal.TryParse(txtDiscount.Text, out decimal discount);

            // Считаем итоговую цену: Базовая цена минус (Базовая цена * Скидка / 100)
            decimal finalPrice = basePrice - (basePrice * discount / 100);

            // Записываем результат обратно в окошко итоговой цены
            txtFinalPrice.Text = finalPrice.ToString("0.00");
        }
        private void Save_Click(object sender, RoutedEventArgs e)
        {
            // проверка на пустоту
            if (string.IsNullOrWhiteSpace(txtName.Text) ||
                string.IsNullOrWhiteSpace(txtPrice.Text) ||
                string.IsNullOrWhiteSpace(txtDiscount.Text)|| string.IsNullOrWhiteSpace(txtConsist.Text) || !txtDiscount.Text.All(char.IsDigit)|| !txtPrice.Text.All(char.IsDigit))
            {
                MessageBox.Show("Заполните название, состав, базовую цену и скидку!");
                return;
            }
            if(Convert.ToDouble(txtDiscount.Text) > 100 )
            {
                MessageBox.Show("Скидка не может быть больше 100%!");
                return;
            }

            if (cmbCategory.SelectedItem == null || cmbManufacturer.SelectedItem == null)
            {
                MessageBox.Show("Обязательно выберите категорию и производителя!");
                return;
            }

            // Лимиты длины
            if (txtName.Text.Length > 100)
            {
                MessageBox.Show("Название слишком длинное! Максимум 100 символов.");
                return;
            }

            if (txtDescription.Text != null && txtDescription.Text.Length > 255)
            {
                MessageBox.Show("Описание слишком большое! Постарайтесь уложиться в 255 символов.");
                return;
            }

            if (txtConsist.Text != null && txtConsist.Text.Length > 255)
            {
                MessageBox.Show("Состав слишком большой! Максимум 255 символов.");
                return;
            }

            // Записываем ID 
            _currentProduct.idCategory = (cmbCategory.SelectedItem as Category).id;
            _currentProduct.idManufacturer = (cmbManufacturer.SelectedItem as Manufacturer).id;

            // блок для фото
            if (_selectedFilePath != null)
            {
                string folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Image");
                Directory.CreateDirectory(folder);

                string fileName = Guid.NewGuid() + Path.GetExtension(_selectedFilePath);
                File.Copy(_selectedFilePath, Path.Combine(folder, fileName));

                _currentProduct.image = fileName;
            }

            // Добавляем новый товар
            if (_currentProduct.id == 0)
            {
                AppConnect.model1.Product.Add(_currentProduct);
            }

            // Сохраняем
            try
            {
                AppConnect.model1.SaveChanges();
                MessageBox.Show("Данные успешно сохранены!");
                NavigationService.GoBack();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при сохранении: " + ex.Message);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }
    }
}