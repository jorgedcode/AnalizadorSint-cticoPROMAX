using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchivoDeTokens
{
    public abstract class NodoExpresion { }

    public class NodoValor : NodoExpresion
    {
        public string TipoToken { get; set; }
        public int Linea { get; set; }
    }

    public class NodoOperacion : NodoExpresion
    {
        public NodoExpresion Izquierdo { get; set; }
        public string Operador { get; set; }
        public NodoExpresion Derecho { get; set; }
    }

    public class NodoUnario : NodoExpresion
    {
        public string Operador { get; set; }      // "OL2"
        public NodoExpresion Operando { get; set; } // árbol que estaba entre los paréntesis
    }
}
