using System.Collections.ObjectModel;
using System.Windows.Input;

namespace MemoApp.UI.MauiApp.Views.Components;

public partial class CustomRangeComponent : ContentView
{
	public static readonly BindableProperty NumberOptionsProperty = 
		BindableProperty.Create(nameof(NumberOptions), typeof(ObservableCollection<string>), typeof(CustomRangeComponent));

	public static readonly BindableProperty SelectedStartIndexProperty = 
		BindableProperty.Create(nameof(SelectedStartIndex), typeof(int), typeof(CustomRangeComponent));

	public static readonly BindableProperty SelectedEndIndexProperty = 
		BindableProperty.Create(nameof(SelectedEndIndex), typeof(int), typeof(CustomRangeComponent));

	public static readonly BindableProperty CustomRangeStartProperty = 
		BindableProperty.Create(nameof(CustomRangeStart), typeof(string), typeof(CustomRangeComponent));

	public static readonly BindableProperty CustomRangeEndProperty = 
		BindableProperty.Create(nameof(CustomRangeEnd), typeof(string), typeof(CustomRangeComponent));

	public static readonly BindableProperty IsStartDropdownVisibleProperty = 
		BindableProperty.Create(nameof(IsStartDropdownVisible), typeof(bool), typeof(CustomRangeComponent));

	public static readonly BindableProperty IsEndDropdownVisibleProperty = 
		BindableProperty.Create(nameof(IsEndDropdownVisible), typeof(bool), typeof(CustomRangeComponent));

	public static readonly BindableProperty ValidationMessageProperty = 
		BindableProperty.Create(nameof(ValidationMessage), typeof(string), typeof(CustomRangeComponent));

	public static readonly BindableProperty HasValidationErrorProperty = 
		BindableProperty.Create(nameof(HasValidationError), typeof(bool), typeof(CustomRangeComponent));

	public static readonly BindableProperty ToggleStartDropdownCommandProperty = 
		BindableProperty.Create(nameof(ToggleStartDropdownCommand), typeof(ICommand), typeof(CustomRangeComponent));

	public static readonly BindableProperty ToggleEndDropdownCommandProperty = 
		BindableProperty.Create(nameof(ToggleEndDropdownCommand), typeof(ICommand), typeof(CustomRangeComponent));

	public static readonly BindableProperty SelectStartNumberOptionCommandProperty = 
		BindableProperty.Create(nameof(SelectStartNumberOptionCommand), typeof(ICommand), typeof(CustomRangeComponent));

	public static readonly BindableProperty SelectEndNumberOptionCommandProperty = 
		BindableProperty.Create(nameof(SelectEndNumberOptionCommand), typeof(ICommand), typeof(CustomRangeComponent));

	public static readonly BindableProperty StartCustomTrainingCommandProperty = 
		BindableProperty.Create(nameof(StartCustomTrainingCommand), typeof(ICommand), typeof(CustomRangeComponent));

	public ObservableCollection<string> NumberOptions
	{
		get => (ObservableCollection<string>)GetValue(NumberOptionsProperty);
		set => SetValue(NumberOptionsProperty, value);
	}

	public int SelectedStartIndex
	{
		get => (int)GetValue(SelectedStartIndexProperty);
		set => SetValue(SelectedStartIndexProperty, value);
	}

	public int SelectedEndIndex
	{
		get => (int)GetValue(SelectedEndIndexProperty);
		set => SetValue(SelectedEndIndexProperty, value);
	}

	public string CustomRangeStart
	{
		get => (string)GetValue(CustomRangeStartProperty);
		set => SetValue(CustomRangeStartProperty, value);
	}

	public string CustomRangeEnd
	{
		get => (string)GetValue(CustomRangeEndProperty);
		set => SetValue(CustomRangeEndProperty, value);
	}

	public bool IsStartDropdownVisible
	{
		get => (bool)GetValue(IsStartDropdownVisibleProperty);
		set => SetValue(IsStartDropdownVisibleProperty, value);
	}

	public bool IsEndDropdownVisible
	{
		get => (bool)GetValue(IsEndDropdownVisibleProperty);
		set => SetValue(IsEndDropdownVisibleProperty, value);
	}

	public string ValidationMessage
	{
		get => (string)GetValue(ValidationMessageProperty);
		set => SetValue(ValidationMessageProperty, value);
	}

	public bool HasValidationError
	{
		get => (bool)GetValue(HasValidationErrorProperty);
		set => SetValue(HasValidationErrorProperty, value);
	}

	public ICommand ToggleStartDropdownCommand
	{
		get => (ICommand)GetValue(ToggleStartDropdownCommandProperty);
		set => SetValue(ToggleStartDropdownCommandProperty, value);
	}

	public ICommand ToggleEndDropdownCommand
	{
		get => (ICommand)GetValue(ToggleEndDropdownCommandProperty);
		set => SetValue(ToggleEndDropdownCommandProperty, value);
	}

	public ICommand SelectStartNumberOptionCommand
	{
		get => (ICommand)GetValue(SelectStartNumberOptionCommandProperty);
		set => SetValue(SelectStartNumberOptionCommandProperty, value);
	}

	public ICommand SelectEndNumberOptionCommand
	{
		get => (ICommand)GetValue(SelectEndNumberOptionCommandProperty);
		set => SetValue(SelectEndNumberOptionCommandProperty, value);
	}

	public ICommand StartCustomTrainingCommand
	{
		get => (ICommand)GetValue(StartCustomTrainingCommandProperty);
		set => SetValue(StartCustomTrainingCommandProperty, value);
	}

	public CustomRangeComponent()
	{
		InitializeComponent();
		// Don't set BindingContext - inherit from parent
	}
}