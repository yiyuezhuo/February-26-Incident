# MapKit

Provides some helpers to make area movement mechanism. 

* Map scrolling
* Selected area highlighting & event.
* A reference data backend using `https://github.com/yiyuezhuo/province-pixel-map-preprocessor`.

## Requirements 

* `YYZLib`(https://github.com/yiyuezhuo/YYZLib). Add it to addons as well.
* `CsvHelper`, `MathNet.Numerics`, `Newtonsoft.Json`

Add following content to `*.csproj`

```xml
  <ItemGroup>
    <PackageReference Include="CsvHelper" Version="27.2.1" />
    <PackageReference Include="MathNet.Numerics" Version="5.0.0-alpha08" />
    <PackageReference Include="Newtonsoft.Json" Version="11.0.2" />
  </ItemGroup>
```

For example:

```xml
<Project Sdk="Godot.NET.Sdk/3.3.0">
  <PropertyGroup>
    <TargetFramework>net472</TargetFramework>
    <RootNamespace>February26Incident</RootNamespace>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="CsvHelper" Version="27.2.1" />
    <PackageReference Include="MathNet.Numerics" Version="5.0.0-alpha08" />
    <PackageReference Include="Newtonsoft.Json" Version="11.0.2" />
  </ItemGroup>
</Project>
```
