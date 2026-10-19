using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
namespace Miracle.Windows;
internal sealed class ToolDialog:Window
{
    public ToolValues Values{get;private set;}=new();
    public ToolDialog(FileTool tool,string input)
    {
        Title="Miracle · "+tool.Name;Width=580;Height=680;MinWidth=400;MinHeight=350;Background=Theme.Ink;Foreground=Theme.White;WindowStartupLocation=WindowStartupLocation.CenterOwner;
        var body=new StackPanel{Margin=new Thickness(24)};Content=new ScrollViewer{Content=body,VerticalScrollBarVisibility=ScrollBarVisibility.Auto};
        body.Children.Add(Theme.Text(tool.Name,30,Theme.Acid,true));body.Children.Add(Theme.Text(tool.Description,14,Theme.White));
        var fields=new Dictionary<string,TextBox>();
        foreach(var field in tool.Fields)
        {
            var label=Theme.Text(field.Label,12,Theme.Muted);label.Margin=new Thickness(0,15,0,5);body.Children.Add(label);
            var box=new TextBox{Text=field.Default,FontSize=16,Padding=new Thickness(8),MaxLength=16000,Background=Theme.Paper,Foreground=Theme.Ink};fields.Add(field.Key,box);body.Children.Add(box);
        }
        var error=Theme.Text("",12,Theme.Brush("#FFB68F"));body.Children.Add(error);
        var row=new WrapPanel{Margin=new Thickness(0,24,0,12)};
        row.Children.Add(Theme.Button("Save a new result ↗",()=>{try{var data=new Dictionary<string,string>();foreach(var pair in fields)data.Add(pair.Key,pair.Value.Text);Values=new ToolValues(data);DialogResult=true;}catch(Exception ex){error.Text=ex.Message;}},Theme.Acid));
        row.Children.Add(Theme.Button("Cancel",()=>DialogResult=false));body.Children.Add(row);
        if(tool.Id.EndsWith(".metadata"))
        {
            var metadata=new TextBox{Text="Reading local metadata…",IsReadOnly=true,TextWrapping=TextWrapping.Wrap,Background=Theme.Panel,Foreground=Theme.White,Padding=new Thickness(10),MaxHeight=220,VerticalScrollBarVisibility=ScrollBarVisibility.Auto};body.Children.Add(metadata);
            var cancellation=new CancellationTokenSource();Closed+=(_,_)=>cancellation.Cancel();
            Loaded+=async(_,_)=>{try{metadata.Text=await FileTools.DescribeAsync(input,cancellation.Token);}catch(OperationCanceledException){}catch(Exception ex){metadata.Text="Could not read metadata: "+ex.Message;}};
        }
    }
}
