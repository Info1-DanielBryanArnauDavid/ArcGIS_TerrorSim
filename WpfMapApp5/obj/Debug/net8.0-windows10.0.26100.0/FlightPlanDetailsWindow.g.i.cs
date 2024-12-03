﻿#pragma checksum "..\..\..\FlightPlanDetailsWindow.xaml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "DF8FF627D8D9283FDDD5CE72812E4B9AC385D3C8"
//------------------------------------------------------------------------------
// <auto-generated>
//     Este código fue generado por una herramienta.
//     Versión de runtime:4.0.30319.42000
//
//     Los cambios en este archivo podrían causar un comportamiento incorrecto y se perderán si
//     se vuelve a generar el código.
// </auto-generated>
//------------------------------------------------------------------------------

using OxyPlot.Wpf;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Controls.Ribbon;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Media.TextFormatting;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Shell;


namespace ArcGIS_App {
    
    
    /// <summary>
    /// FlightPlanDetailsWindow
    /// </summary>
    public partial class FlightPlanDetailsWindow : System.Windows.Window, System.Windows.Markup.IComponentConnector {
        
        
        #line 63 "..\..\..\FlightPlanDetailsWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Label WatermarkLabel;
        
        #line default
        #line hidden
        
        
        #line 66 "..\..\..\FlightPlanDetailsWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBox SearchBox;
        
        #line default
        #line hidden
        
        
        #line 82 "..\..\..\FlightPlanDetailsWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.ListBox FlightPlansList;
        
        #line default
        #line hidden
        
        
        #line 83 "..\..\..\FlightPlanDetailsWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button GoToButton;
        
        #line default
        #line hidden
        
        
        #line 101 "..\..\..\FlightPlanDetailsWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBlock FlightPlanDetailsText;
        
        #line default
        #line hidden
        
        
        #line 106 "..\..\..\FlightPlanDetailsWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal OxyPlot.Wpf.PlotView VerticalProfilePlot;
        
        #line default
        #line hidden
        
        private bool _contentLoaded;
        
        /// <summary>
        /// InitializeComponent
        /// </summary>
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "9.0.0.0")]
        public void InitializeComponent() {
            if (_contentLoaded) {
                return;
            }
            _contentLoaded = true;
            System.Uri resourceLocater = new System.Uri("/ArcGIS_App;V1.0.0.0;component/flightplandetailswindow.xaml", System.UriKind.Relative);
            
            #line 1 "..\..\..\FlightPlanDetailsWindow.xaml"
            System.Windows.Application.LoadComponent(this, resourceLocater);
            
            #line default
            #line hidden
        }
        
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "9.0.0.0")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily")]
        void System.Windows.Markup.IComponentConnector.Connect(int connectionId, object target) {
            switch (connectionId)
            {
            case 1:
            this.WatermarkLabel = ((System.Windows.Controls.Label)(target));
            return;
            case 2:
            this.SearchBox = ((System.Windows.Controls.TextBox)(target));
            
            #line 67 "..\..\..\FlightPlanDetailsWindow.xaml"
            this.SearchBox.KeyUp += new System.Windows.Input.KeyEventHandler(this.SearchBox_KeyUp);
            
            #line default
            #line hidden
            
            #line 67 "..\..\..\FlightPlanDetailsWindow.xaml"
            this.SearchBox.GotFocus += new System.Windows.RoutedEventHandler(this.SearchBox_GotFocus);
            
            #line default
            #line hidden
            
            #line 67 "..\..\..\FlightPlanDetailsWindow.xaml"
            this.SearchBox.LostFocus += new System.Windows.RoutedEventHandler(this.SearchBox_LostFocus);
            
            #line default
            #line hidden
            return;
            case 3:
            
            #line 73 "..\..\..\FlightPlanDetailsWindow.xaml"
            ((System.Windows.Controls.Button)(target)).Click += new System.Windows.RoutedEventHandler(this.SortByName_Click);
            
            #line default
            #line hidden
            return;
            case 4:
            
            #line 76 "..\..\..\FlightPlanDetailsWindow.xaml"
            ((System.Windows.Controls.Button)(target)).Click += new System.Windows.RoutedEventHandler(this.SortByDeparture_Click);
            
            #line default
            #line hidden
            return;
            case 5:
            this.FlightPlansList = ((System.Windows.Controls.ListBox)(target));
            
            #line 82 "..\..\..\FlightPlanDetailsWindow.xaml"
            this.FlightPlansList.SelectionChanged += new System.Windows.Controls.SelectionChangedEventHandler(this.FlightPlansList_SelectionChanged);
            
            #line default
            #line hidden
            return;
            case 6:
            this.GoToButton = ((System.Windows.Controls.Button)(target));
            
            #line 84 "..\..\..\FlightPlanDetailsWindow.xaml"
            this.GoToButton.Click += new System.Windows.RoutedEventHandler(this.GoToButton_Click);
            
            #line default
            #line hidden
            return;
            case 7:
            this.FlightPlanDetailsText = ((System.Windows.Controls.TextBlock)(target));
            return;
            case 8:
            this.VerticalProfilePlot = ((OxyPlot.Wpf.PlotView)(target));
            return;
            }
            this._contentLoaded = true;
        }
    }
}

