.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )
  .ver 4:0:0:0
}
.assembly HelloLib
{
  .ver 1:0:0:0
}
.module HelloLib.dll

.class public auto ansi beforefieldinit HelloLib.Greeter
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname rtspecialname
          instance void  .ctor() cil managed
  {
    .maxstack 8
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }

  .method public hidebysig static string
          Greet(string name) cil managed
  {
    .maxstack 8
    ldstr      "Merhaba, "
    ldarg.0
    call       string [mscorlib]System.String::Concat(string, string)
    ret
  }
}

.class public auto ansi beforefieldinit GlobalType
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname rtspecialname
          instance void  .ctor() cil managed
  {
    .maxstack 8
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }
}
