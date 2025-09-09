using System.Collections.ObjectModel;
using System.Windows.Input;

namespace MemoApp.UI.MauiApp.Views.Components;

public partial class NumberSelectorComponent : ContentView
{
	public static readonly BindableProperty TitleProperty = 
		BindableProperty.Create(nameof(Title), typeof(string), typeof(NumberSelectorComponent));

	public static readonly BindableProperty NumberOptionsProperty = 
		BindableProperty.Create(nameof(NumberOptions), typeof(ObservableCollection<string>), typeof(NumberSelectorComponent));

	public static readonly BindableProperty SelectedIndexProperty = 
		BindableProperty.Create(nameof(SelectedIndex), typeof(int), typeof(NumberSelectorComponent));

	public static readonly BindableProperty SelectedValueProperty = 
		BindableProperty.Create(nameof(SelectedValue), typeof(string), typeof(NumberSelectorComponent));

	public static readonly BindableProperty IsDropdownVisibleProperty = 
		BindableProperty.Create(nameof(IsDropdownVisible), typeof(bool), typeof(NumberSelectorComponent));

	public static readonly BindableProperty ToggleDropdownCommandProperty = 
		BindableProperty.Create(nameof(ToggleDropdownCommand), typeof(ICommand), typeof(NumberSelectorComponent));

	public static readonly BindableProperty SelectNumberOptionCommandProperty = 
		BindableProperty.Create(nameof(SelectNumberOptionCommand), typeof(ICommand), typeof(NumberSelectorComponent));

	public static readonly BindableProperty AccessibilityNameProperty = 
		BindableProperty.Create(nameof(AccessibilityName), typeof(string), typeof(NumberSelectorComponent));

	public static readonly BindableProperty AccessibilityHelpProperty = 
		BindableProperty.Create(nameof(AccessibilityHelp), typeof(string), typeof(NumberSelectorComponent));

	public string Title
	{
		get => (string)GetValue(TitleProperty);
		set => SetValue(TitleProperty, value);
	}

	public ObservableCollection<string> NumberOptions
	{
		get => (ObservableCollection<string>)GetValue(NumberOptionsProperty);
		set => SetValue(NumberOptionsProperty, value);
	}

	public int SelectedIndex
	{
		get => (int)GetValue(SelectedIndexProperty);
		set => SetValue(SelectedIndexProperty, value);
	}

	public string SelectedValue
	{
		get => (string)GetValue(SelectedValueProperty);
		set => SetValue(SelectedValueProperty, value);
	}

	public bool IsDropdownVisible
	{
		get => (bool)GetValue(IsDropdownVisibleProperty);
		set => SetValue(IsDropdownVisibleProperty, value);
	}

	public ICommand ToggleDropdownCommand
	{
		get => (ICommand)GetValue(ToggleDropdownCommandProperty);
		set => SetValue(ToggleDropdownCommandProperty, value);
	}

	public ICommand SelectNumberOptionCommand
	{
		get => (ICommand)GetValue(SelectNumberOptionCommandProperty);
		set => SetValue(SelectNumberOptionCommandProperty, value);
	}

	public string AccessibilityName
	{
		get => (string)GetValue(AccessibilityNameProperty);
		set => SetValue(AccessibilityNameProperty, value);
	}

	public string AccessibilityHelp
	{
		get => (string)GetValue(AccessibilityHelpProperty);
		set => SetValue(AccessibilityHelpProperty, value);
	}

	public NumberSelectorComponent()
	{
		InitializeComponent();
		BindingContext = this;
	}
}