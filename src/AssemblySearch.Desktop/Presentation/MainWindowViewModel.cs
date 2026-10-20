using System.Collections.ObjectModel;using AssemblySearch.Core.Query;using AssemblySearch.Core.Search;
namespace AssemblySearch.Desktop.Presentation;
public sealed class MainWindowViewModel(SearchController controller){
 public ObservableCollection<string> LoadedPaths{get;}=[];
 public SearchController Controller{get;}=controller;
 public SearchQuery Query{get;set;}=new("");
 public SearchResult? SelectedResult{get;set;}
 public ObservableCollection<SearchResult> Results=>Controller.Results;
 public Task SearchAsync(bool immediate=false)=>immediate?Controller.SubmitAsync(new(Query,LoadedPaths.ToArray())):Controller.UpdateAsync(new(Query,LoadedPaths.ToArray()));
}
