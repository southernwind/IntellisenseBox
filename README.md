Visual Studio風AutoCompleteBox(?)

### Sample 1 (Auto)
``` C
this.Source = new ObservableCollection<ICandidate>(new[]
    {
        "aaa",
        "bbb",
        "ccc",
        "active",
        "apple",
        "acid",
        "access",
        "across"
    }.Select(x => new Candidate(x)));
```

``` xml
<controls:IntellisenseBox 
    Width="300"
    Height="100"
    SuggestSource="{Binding Source}"
    Separator=" ,&#xd;&#xa;"
    Mode="Auto"
    AcceptsReturn="True"
    IgnoreCase="True"/>
```

![Sample1](https://raw.githubusercontent.com/southernwind/Images/master/IntellisenseBox/Sample1.gif)


### Sample2 (Manual)
``` C
// this.Source2 = new ObservableCollection<ICandidate>();

private string _currentText;
public string CurrentText {
    get { return this._currentText; }
    set {
        if (this._currentText == value) {
            return;
        }

        this._currentText = value;
        this.Source2.Clear();
        foreach (var w in this.Source.Where(x => !x.Word.ToLower().Contains(this.CurrentText.ToLower()))) {
            this.Source2.Add(w);
        }
    }
}
```

``` xml
<controls:IntellisenseBox 
    Width="300"
    Height="100"
    SuggestSource="{Binding Source2}"
    Separator=" ,&#xd;&#xa;"
    Mode="Manual"
    CurrentText="{Binding CurrentText,Mode=OneWayToSource}"/>
```
![Sample2](https://raw.githubusercontent.com/southernwind/Images/master/IntellisenseBox/Sample2.gif)