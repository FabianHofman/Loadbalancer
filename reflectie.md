Ik heb gebruik gemaakt van het MVVM pattern om zo de UI te kunnen scheiden van de achterliggende logica. Verder is hier te zien dat ik geprobeerd heb MVC toe te passen. Dit door de Loadbalancer de classe te maken, de LoadbalancerViewModel de model te maken en de MainWindow als view zijnde. D.m.v. het gebruik maken van een CommandDelegate kan ik een Delegate meegeven die uitgevoerd zal worden zodra de Predicate die meegegeven wordt dit goedkeurd. Hieronder een voorbeeldje.
```csharp
_clearLog = new CommandDelegate(OnClearLog, CanClearLog);
```
In de OnClearLog zal de Delegate meegegeven worden die uitgevoerd moet worden. In mijn geval is dat:
```csharp
public void OnClearLog(object commandParameter) => _loadbalancer.ClearLog();
```
Tevens geven we een Predicate mee waar eerst aan voldaan moet worden voordat de Delegate uitgevoerd kan worden. In ons geval kan dit altijd dus zal de code er ongeveer zo uit zien:
```csharp
private bool CanClearLog(object commandParameter) => true;
```

Verder heb ik gebruik gemaakt van het interface IAlgorithm (zelf gemaakt) om hier algoritmes te kunnen koppelen en dit in run-time uit te kunnen lezen in het programma. 

Ik heb de algoritmes en de persistence samengevoegd (dus persistence als algoritme zijnde). De persistence staat namelijk verbonden aan het RoundRobin algoritme dat is toegepast (voor als er geen cookie of sessie aanwezig is). Hier ben ik zelf wat minder tevreden om, maar om vele if-checks te voorkomen was dit een van de beter oplossingen. 

Verder maakt groot en deel van de applicatie gebruik van Tasks. Hiermee worden berekeningen en non-UI bezigheden op een andere thread uitgevoerd om zo blokkering op de UI thread te voorkomen. Hiermee zal de UI niet blokkeren en blijft de applicatie responsive.

Ik heb geprobeerd zo veel mogelijk te scheiden. Zoals in het klasse diagram te zien is, zijn er verschillende Classes aangemaakt om wat data op te slaan. Server en Session zijn hier een goed voorbeeld van. Een Server bestaat uit 3 properties (Host, Port en Alive). Verder is de Server klasse verantwoordelijk om te vragen hoe het met de Servers gaat. Dit staat in de functie `AskForHealth()`. Hierin wordt er een request gemaakt die er ongeveer zo uit ziet:
```csharp
StringBuilder builder = new StringBuilder();
builder.AppendLine("GET / HTTP/1.1");
builder.AppendLine($"Host: {Host}");
builder.AppendLine("Connection: close");
builder.AppendLine();
byte[]  header = Encoding.ASCII.GetBytes(builder.ToString());
```
Vervolgens zal er een connectie gemaakt worden met de server en dit request gestuurd worden. In het response dat we krijgen checken we of er er `"200 OK"` status terug komt. Zodra dit gebeurd weten we dat de server het goed doet. Als dit niet gebeurd, dan zal de `Server.Alive` op `false` gezet worden en is deze server niet meer te gebruiken.

Bij de Session zal het IP opgeslagen worden van de gebruiker en het IP van de server. Hiermee kunnen we een link leggen en dit iedere keer terug geven. 

Over het algemeen ben ik zeer tevreden met dit product. Alles verloopt volgens de criteria die opgesteld is door school en alles wat ik tijdens de lessen heb geleerd heb ik toegepast.

In de toekomst zal ik dit nooit meer zo bouwen. Aangezien we het HTTP protocol hebben en daar packages voor hebben zal ik dit eerder gebruiken. Ik heb wel meer inzicht gekregen in hoe dit protocol werkt en ben zeer tevreden met mijn eigen opgestelde protocol.

![Class diagram](./Images/ClassDiagram.png)