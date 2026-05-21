using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ArchivoDeTokens
{
    public partial class Form1 : Form
    {
        private List<ErrorLexico> listaErrores = new List<ErrorLexico>();
        private List<Simbolo> listaSimbolos = new List<Simbolo>();
        private Dictionary<int, Dictionary<string, string>> matrizTransicion = new Dictionary<int, Dictionary<string, string>>();
        private int contadorSimbolos = 1;
        private int contadorLinea = 2;
        private int contE = 0;

        public Form1()
        {
            InitializeComponent();
            CargarMatrizEnMemoria();
            lblEquipo.Text = "Equipo\nHiram García Guerra. #23100161\nJorge Arturo Mata Camacho. #C21100514\nReynaldo Daniel Reyes Parra. #23100202\n\nVersión: 1.3";
            rtxTokens.Text = "1\n";
            rtxLineasCodigo.Text = "1\n";
        }

        private void CargarMatrizEnMemoria()
        {
            using (SqlConnection con = ConexionBD.ObtenerConexion())
            {
                string query = "SELECT * FROM matriz";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    con.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int estadoFila = Convert.ToInt32(reader["ESTADO"]);

                            var filaActual = new Dictionary<string, string>();

                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                string nombreColumna = reader.GetName(i);

                                if (nombreColumna != "ESTADO")
                                {
                                    string valorCelda = reader[i].ToString();
                                    filaActual[nombreColumna] = valorCelda;
                                }
                            }

                            matrizTransicion[estadoFila] = filaActual;
                        }
                    }
                }
            }
        }


        #region ANALIZADOR SINTÁCTICO (TOP-DOWN)

        public class TokenSintactico
        {
            public string Valor { get; set; }
            public int Linea { get; set; }
        }

        public class ErrorSintactico
        {
            public int Linea { get; set; }
            public string Descripcion { get; set; }
        }

        public class ExcepcionSintactica : Exception
        {
            public ExcepcionSintactica(string msg) : base(msg) { }
        }

        private int punteroSintactico = 0;
        private List<TokenSintactico> tokensSintacticosObj = new List<TokenSintactico>();
        private List<ErrorSintactico> listaErroresSintacticos = new List<ErrorSintactico>();
        private StringBuilder traduccionSintactica = new StringBuilder();
        private StringBuilder trazaTopDown = new StringBuilder();
        private List<string> pasosTraza = new List<string>();
        private void RegistrarPasoTopDown(string paso)
        {
            pasosTraza.Add(paso);
        }
        private string ObtenerTokensConsumidos(int inicio, int fin)
        {
            List<string> consumidos = new List<string>();
            for (int i = inicio; i < fin && i < tokensSintacticosObj.Count; i++)
            {
                consumidos.Add(tokensSintacticosObj[i].Valor);
            }
            return string.Join(" ", consumidos);
        }
        Dictionary<string, string> palabrasReservadas = new Dictionary<string, string>
        {
            ["PR1"] = "INI",
            ["PR2"] = "FIN",
            ["PR3"] = "NVO",
            ["PR4"] = "COMP",
            ["PR5"] = "FLOT",
            ["OA1"] = "+",
            ["OA2"] = "-",
            ["OA3"] = "*",
            ["OA4"] = "/",
            ["OA5"] = "ALA",
            ["PR7"] = "CICLO",
            ["PR8"] = "MTS",
            ["PR9"] = "VE",
            ["PR10"] = "DETEN",
            ["PR11"] = "SI",
            ["PR12"] = "SINO",
            ["PR13"] = "VIENE",
            ["PR14"] = "VA",
            ["PR15"] = "LOGI",
            ["PR16"] = "LEG",
            ["PR17"] = "MENT",
            ["PR18"] = "MTX",
            ["PR19"] = "PTX",
            ["PR20"] = "TBM",
            ["PR21"] = "OLO",
            ["PR22"] = "NAH",
            ["PR23"] = "NINT",
            ["PR24"] = "CASO",
            ["PR25"] = "LVC",
            ["ce07"] = "(",
            ["ce08"] = ")",
            ["ce09"] = "{",
            ["ce10"] = "}",
            ["ce11"] = ":",
        };


        // Retorna el token en la posición actual
        private string TokenActual()
        {
            if (punteroSintactico < tokensSintacticosObj.Count)
                return tokensSintacticosObj[punteroSintactico].Valor;
            return "EOF";
        }

        // Retorna la línea del token actual
        private int LineaActual()
        {
            if (punteroSintactico < tokensSintacticosObj.Count)
                return tokensSintacticosObj[punteroSintactico].Linea;
            return tokensSintacticosObj.Count > 0 ? tokensSintacticosObj.Last().Linea : 0;
        }

        private string TokenSiguiente()
        {
            if (punteroSintactico + 1 < tokensSintacticosObj.Count)
                return tokensSintacticosObj[punteroSintactico + 1].Valor;
            return "EOF";
        }

        private void RegistrarTraduccion(string regla)
        {
            traduccionSintactica.AppendLine($"Línea {LineaActual()}: {regla}");
        }

        // Verifica y avanza el puntero
        private void Match(string esperado)
        {
            string actual = TokenActual();

            if (actual == "EOF")
                throw new ExcepcionSintactica($"Se llegó al final inesperadamente. Faltó: '{esperado}'");

            if (esperado == "ID" && actual.StartsWith("IDENT")) { punteroSintactico++; return; }
            if (esperado == "opa" && (actual == "opa" || actual == "OPA" || actual == "OPAS")) { punteroSintactico++; return; }
            if (esperado == "CNU" && (actual == "CNU" || actual == "CN")) { punteroSintactico++; return; }

            if (actual == esperado)
            {
                punteroSintactico++;
            }
            else
            {
                if (esperado.StartsWith("PR") || esperado.StartsWith("OA") || esperado.StartsWith("ce"))
                {
                    throw new ExcepcionSintactica($"Se esperaba '{palabrasReservadas[esperado]}', pero se encontró '{actual}'.");
                }
                else
                {
                    throw new ExcepcionSintactica($"Se esperaba '{esperado}', pero se encontró '{actual}'.");
                }
            }
        }


        private void ParsePrograma()
        {
            RegistrarTraduccion("INICIO_PROGRAMA -> PR1");
            Match("PR1"); // INI

            ParseInstruccionesBloque(); // INSTRUCCIONES

            RegistrarTraduccion("FIN_PROGRAMA -> PR2");
            Match("PR2"); // FIN
        }

        private void ParseInstruccionesBloque()
        {
            // Ejecuta instrucciones mientras no se llegue a cierres de bloque o fin de programa
            while (TokenActual() != "PR2" && TokenActual() != "ce10" && TokenActual() != "PR10" && TokenActual() != "EOF")
            {
                try
                {
                    ParseInstruccion();
                }
                catch (ExcepcionSintactica ex)
                {
                    // Acumular error y recuperar el parser (Modo Pánico)
                    listaErroresSintacticos.Add(new ErrorSintactico { Linea = LineaActual(), Descripcion = ex.Message });
                    RecuperarModoPanico();
                }
            }
        }

        private void RecuperarModoPanico()
        {
            while (TokenActual() != "FDL" && TokenActual() != "ce10" && TokenActual() != "ce09" && TokenActual() != "EOF")
            {
                punteroSintactico++;
            }

            if (TokenActual() == "FDL")
            {
                punteroSintactico++;
            }
            else if (TokenActual() == "ce09")
            {
                punteroSintactico++;
                ParseInstruccionesBloque();
                if (TokenActual() == "ce10")
                {
                    punteroSintactico++;
                }
            }
        }

        private void ParseInstruccion()
        {
            string t = TokenActual();

            // Ruteador Principal de Instrucciones original
            if (t == "PR3" || t == "PR03") ParseIN01();
            else if (t == "PR4" || t == "PR04") ParseIN02();
            else if (t == "PR5" || t == "PR05") ParseIN03();
            else if (t == "PR07" || t == "PR7") ParseIN05();
            else if (t == "PR08" || t == "PR8") ParseIN06();
            else if (t == "PR13") ParseIN07();
            else if (t == "PR14") ParseIN08();
            else if (t == "PR15") ParseIN09();
            else if (t == "PR18") ParseIN10();
            else if (t == "PR19") ParseIN11();
            else if (t == "OL2") ParseIN14();
            else if (t == "PR11") ParseIN15();
            else if (t == "PR9" || t == "PR09") ParseIN16();
            else if (t == "PR23") ParseIN17();
            else if (t.StartsWith("IDENT") || t == "ID")
            {
                ParseAsignacion();
            }
            else if (t.StartsWith("IDENT") || t == "CNU" || t == "CN")
            {
                string next = TokenSiguiente();
                if (next == "OA5" || next == "OA05") ParseIN04();
                else if (next == "OL1") ParseIN12();
                else if (next == "OL3") ParseIN13();
                else throw new ExcepcionSintactica($"Estructura no reconocida comenzando con '{t}'");
            }
            else
            {
                throw new ExcepcionSintactica($"No se reconoce el inicio de la instrucción: '{t}'");
            }
        }

        private void ParseIN01()
        {
            RegistrarTraduccion("IN01 -> PR03 ID opa ARG1 FDL");
            Match(TokenActual()); Match("ID"); Match("opa"); ParseARG1(); Match("FDL");
        }

        private void ParseIN02()
        {
            string msg = "IN02 -> PR04 ID";
            Match(TokenActual()); Match("ID");
            if (TokenActual().StartsWith("OPA") || TokenActual() == "opa") { Match("opa"); ParseOPAR(); msg += " [OPA OPAR]"; }
            msg += " FDL";
            RegistrarTraduccion(msg);
            Match("FDL");
        }

        private void ParseIN03()
        {
            string msg = "IN03 -> PR05 ID";
            Match(TokenActual()); Match("ID");
            if (TokenActual().StartsWith("OPA") || TokenActual() == "opa") { Match("opa"); ParseOPAR(); msg += " [OPA OPAR]"; }
            msg += " FDL";
            RegistrarTraduccion(msg);
            Match("FDL");
        }

        private void ParseIN04()
        {
            RegistrarTraduccion("IN04 -> ARG4 PR6 ARG4 FDL");
            ParseARG4();
            if (TokenActual() == "PR6" || TokenActual() == "PR06")
            {
                Match(TokenActual());
            }
            else
            {
                throw new ExcepcionSintactica($"Se esperaba PR6 (ALA) entre los argumentos, se encontró {TokenActual()}");
            }
            ParseARG4();
            Match("FDL");
        }

        private void ParseIN05()
        {
            RegistrarTraduccion("IN05 (PARA) -> PR07 ce07 PR4 ID opa ARG2 ce18 CONDIC ce18 INCRE ce08 ce09 INSTR ce10");
            Match(TokenActual()); Match("ce07");
            Match("PR4"); Match("ID"); Match("opa"); ParseARG2(); // ARG5 original
            Match("ce18"); ParseCONDIC(); Match("ce18");
            Match("ID"); Match("opa"); ParseOPAR(); // INCRE original
            Match("ce08"); Match("ce09"); ParseInstruccionesBloque(); Match("ce10");
        }

        private void ParseIN06()
        {
            RegistrarTraduccion("IN06 (MIENTRAS) -> PR08 ce07 CONDIC ce08 ce09 INSTR ce10");
            Match(TokenActual()); Match("ce07"); ParseCONDIC(); Match("ce08"); Match("ce09"); ParseInstruccionesBloque(); Match("ce10");
        }

        private void ParseIN07()
        {
            RegistrarTraduccion("IN07 (ENTRADA) -> PR13 ce07 ID ce08 FDL");
            Match(TokenActual()); Match("ce07"); Match("ID"); Match("ce08"); Match("FDL");
        }

        private void ParseIN08()
        {
            RegistrarTraduccion("IN08 (IMPRIMIR) -> PR14 ce07 ARG7 ce08 FDL");
            Match(TokenActual()); Match("ce07");
            string t = TokenActual();
            if (t.StartsWith("IDENT")) Match("ID");
            else if (t == "CNU" || t == "CN" || t == "CAD" || t == "CAR") Match(t);
            else throw new ExcepcionSintactica($"Argumento no válido para VA: {t}");
            Match("ce08"); Match("FDL");
        }

        private void ParseIN09()
        {
            string msg = "IN09 (BOOL) -> PR15 ID";
            
            Match(TokenActual()); Match("ID");
            if (TokenActual().StartsWith("OPA") || TokenActual() == "opa")
            {
                Match("opa");
                if (TokenActual() == "PR16" || TokenActual() == "PR17") Match(TokenActual());
                else if (TokenActual() == "OL2") ParseIN14();
                else throw new ExcepcionSintactica("Se esperaba LEG o MENT para booleano.");
                msg += " [opa PR16 | PR17 | OL2]";
            }
            msg += " FDL";
            RegistrarTraduccion(msg);
            Match("FDL");
        }

        private void ParseIN10()
        {
            string msg = "IN10 (CADENA) -> PR18 ID";
            Match(TokenActual()); Match("ID");
            if (TokenActual().StartsWith("OPA") || TokenActual() == "opa") { Match("opa"); Match("CAD"); msg += " [opa CAD]"; }
            msg += " FDL";
            RegistrarTraduccion(msg);
            Match("FDL");
        }

        private void ParseIN11()
        {
            string msg = "IN11 (CARACTER) -> PR19 ID";
            Match(TokenActual()); Match("ID");
            if (TokenActual().StartsWith("OPA") || TokenActual() == "opa")
            {
                Match("opa");
                if (TokenActual() == "CAR" || TokenActual() == "CAD") Match(TokenActual());
                msg += " [opa CAR|CAD]";
            }
            msg += " FDL";
            RegistrarTraduccion(msg);
            Match("FDL");
        }

        private void ParseIN12() { RegistrarTraduccion("IN12 -> CONDIC PR20 CONDIC"); ParseCONDIC(); Match("PR20"); ParseCONDIC(); }
        private void ParseIN13() { RegistrarTraduccion("IN13 -> CONDIC PR21 CONDIC"); ParseCONDIC(); Match("PR21"); ParseCONDIC(); }
        private void ParseIN14() { RegistrarTraduccion("IN14 -> OL2 ce07 CONDIC ce08"); Match(TokenActual()); Match("ce07"); ParseCONDIC(); Match("ce08"); }

        private void ParseIN15()
        {
            RegistrarPasoTopDown("\n=== INICIANDO REDUCCIÓN TOP-DOWN PARA SI/SINO ===");

            string tPR11 = TokenActual();
            Match("PR11");
            string tParAb = TokenActual();
            Match("ce07");

            RegistrarPasoTopDown($"1) {tPR11} {tParAb} ... (Evaluando condición interior)");

            // Capturamos dónde empieza la condición
            int inicioCondic = punteroSintactico;
            ParseCONDIC(); // Descendemos
            int finCondic = punteroSintactico; // Capturamos dónde terminó

            // Extraemos los tokens exactos que formaron la condición
            string tokensCondic = ObtenerTokensConsumidos(inicioCondic, finCondic);
            RegistrarPasoTopDown($"   -> Los tokens [ {tokensCondic} ] se reducen a: CONDIC");
            RegistrarPasoTopDown($"2) {tPR11} {tParAb} CONDIC ...");

            string tParCe = TokenActual();
            Match("ce08");
            string tLlaAb = TokenActual();
            Match("ce09");

            RegistrarPasoTopDown($"3) {tPR11} {tParAb} CONDIC {tParCe} {tLlaAb} ... (Evaluando bloque interno)");

            int inicioInstr = punteroSintactico;
            ParseInstruccionesBloque();
            int finInstr = punteroSintactico;

            string tokensInstr = ObtenerTokensConsumidos(inicioInstr, finInstr);
            RegistrarPasoTopDown($"   -> Los tokens [ {tokensInstr} ] se reducen a: INSTR");
            RegistrarPasoTopDown($"4) {tPR11} {tParAb} CONDIC {tParCe} {tLlaAb} INSTR ...");

            string tLlaCe = TokenActual();
            Match("ce10");

            RegistrarPasoTopDown($"5) {tPR11} {tParAb} CONDIC {tParCe} {tLlaAb} INSTR {tLlaCe}");
            RegistrarPasoTopDown("6) ---> Se reduce a: IN15 (Estructura SI completada)");

            if (TokenActual() == "PR12")
            {
                RegistrarPasoTopDown("\n--- EXTENSIÓN SINO DETECTADA ---");
                string tPR12 = TokenActual();
                Match("PR12");
                string tLlaAbSino = TokenActual();
                Match("ce09");

                RegistrarPasoTopDown($"7) {tPR12} {tLlaAbSino} ... (Evaluando bloque SINO)");

                int inicioInstrSino = punteroSintactico;
                ParseInstruccionesBloque();
                int finInstrSino = punteroSintactico;

                string tokensInstrSino = ObtenerTokensConsumidos(inicioInstrSino, finInstrSino);
                RegistrarPasoTopDown($"   -> Los tokens [ {tokensInstrSino} ] se reducen a: INSTR");

                string tLlaCeSino = TokenActual();
                Match("ce10");

                RegistrarPasoTopDown($"8) {tPR12} {tLlaAbSino} INSTR {tLlaCeSino}");
                RegistrarPasoTopDown("9) ---> Se reduce a: IN15 (Estructura SI-SINO completada)");
            }
        }
        /*
        private void ParseIN15()
        {
            string msg = "IN15 (SI";
            Match(TokenActual()); Match("ce07"); ParseCONDIC(); Match("ce08"); Match("ce09"); ParseInstruccionesBloque(); Match("ce10");
            if (TokenActual() == "PR12")
            {
                Match("PR12"); Match("ce09"); ParseInstruccionesBloque(); Match("ce10");
                msg += "-NO";
            }
            msg += ") -> PR11 ce07 CONDIC ce08 ce09 INSTR ce10 [PR12 ce09 INSTR ce10]";
            RegistrarTraduccion(msg);
        }
        */

        private void ParseIN16()
        {
            RegistrarTraduccion("IN16 (HAZ-MIENTRAS) -> PR9 ce09 INSTR ce10 PR8 ce07 CONDIC ce08 FDL");
            Match(TokenActual()); Match("ce09"); ParseInstruccionesBloque(); Match("ce10");
            if (TokenActual() == "PR8" || TokenActual() == "PR08") Match(TokenActual());
            Match("ce07"); ParseCONDIC(); Match("ce08"); Match("FDL");
        }

        private void ParseIN17()
        {
            RegistrarTraduccion("IN17 (SWITCH) -> PR23 ce07 ID ce08 ce09 [PR24 ARG12 ce11 INSTR PR10 FDL] [PR25 ce11 INSTR PR10 FDL] ce10");
            Match(TokenActual()); Match("ce07"); Match("ID"); Match("ce08"); Match("ce09");
            while (TokenActual() == "PR24")
            {
                Match("PR24");
                string t = TokenActual();
                if (t == "CNU" || t == "CN" || t == "CAD" || t == "CAR") Match(t);
                Match("ce11"); ParseInstruccionesBloque(); Match("PR10"); Match("FDL");
            }
            if (TokenActual() == "PR25")
            {
                Match("PR25"); Match("ce11"); ParseInstruccionesBloque(); Match("PR10");
                if (TokenActual() == "FDL") Match("FDL");
            }
            Match("ce10");
        }

        private void ParseAsignacion()
        {
            RegistrarTraduccion("ASIGNACION -> ID opa EXPRESION FDL");
            if (TokenActual().StartsWith("IDENT") || TokenActual() == "ID")
            {
                Match(TokenActual()); // Consume el identificador
            }
            else
            {
                throw new ExcepcionSintactica("Se esperaba un identificador para iniciar la asignación.");
            }

            if (TokenActual() == "OPA" || TokenActual() == "opa")
            {
                Match(TokenActual()); // Consume el '='
            }
            else
            {
                throw new ExcepcionSintactica("Se esperaba el operador de asignación '='.");
            }

            ParseExpresion();
            Match("FDL");
        }

        // --- EVALUADORES DE EXPRESIONES (OPAR y CONDIC) ---
        private void ParseARG1()
        { // ID | CNU | CAD | OPAR | PR14 | PR15
            string t = TokenActual();
            if (t == "CAD" || t == "PR14" || t == "PR15") Match(t);
            else ParseOPAR(); // OPAR internamente resuelve ID o CNU
        }

        private void ParseARG2()
        { // DIG | ID | OPAR
            ParseOPAR(); // OPAR resuelve ID o Número entero
        }

        private void ParseARG3()
        { // CNU | ID | OPAR
            ParseOPAR(); // OPAR resuelve ID o Número flotante
        }

        private void ParseARG4()
        { // ID | CNU
            string t = TokenActual();
            if (t.StartsWith("IDENT")) Match("ID");
            else if (t == "CNU" || t == "CN") Match(t);
            else throw new Exception($"ARG4 inválido: {t}");
        }

        private void ParseARG5()
        { // PR04 ID OPA ARG2 (Utilizado en ciclo PARA)
            Match("PR04"); Match("ID"); Match("opa"); ParseARG2();
        }

        private void ParseARG6()
        { // ID
            Match("ID");
        }

        private void ParseARG7()
        { // ID | CNU | CAD | CAR
            string t = TokenActual();
            if (t.StartsWith("IDENT")) Match("ID");
            else if (t == "CNU" || t == "CN" || t == "CAD" || t == "CAR") Match(t);
            else throw new Exception($"ARG7 inválido: {t}");
        }

        private void ParseARG8()
        { // PR16 | PR17 (Verdadero o Falso)
            string t = TokenActual();
            if (t == "PR16" || t == "PR17") Match(t);
            else throw new Exception($"Se esperaba LEG o MENT, se encontró {t}");
        }

        private void ParseARG9()
        { // CAD
            Match("CAD");
        }

        private void ParseARG10()
        { // CAR (o su equivalente léxico)
            string t = TokenActual();
            if (t == "CAR" || t == "CAD") Match(t);
            else throw new Exception($"ARG10 inválido: {t}");
        }

        private void ParseARG11()
        { // IDBOOL | PR16 | PR17 | OPREL | OPLOG
            ParseCONDIC();
        }

        private void ParseARG12()
        { // CNU | CAD | CAR
            string t = TokenActual();
            if (t == "CNU" || t == "CN" || t == "CAD" || t == "CAR") Match(t);
            else throw new Exception($"ARG12 inválido en CASO: {t}");
        }

        private void ParseARG13()
        { // PR24 ARG12 ce11 INSTR PR10 del ARG13 | épsilon
            if (TokenActual() == "PR24")
            {
                Match("PR24"); ParseARG12(); Match("ce11");
                ParseInstruccionesBloque(); // INSTR
                Match("PR10"); Match("FDL");

                ParseARG13();
            }
        }

        private void ParseExpresion()
        {
            string t = TokenActual();

            // Si empieza con comillas o es una constante de texto/cadena
            if (t == "CAD" || t.StartsWith("cad"))
            {
                ParseValorCadena();
            }
            // Si es un número (CNU o CN) o abre un paréntesis, probablemente es una operación matemática
            else if (t == "CNU" || t == "CN" || t == "ce07" || t.StartsWith("IDENT"))
            {
                // Evaluamos si es una expresión aritmética o un ID solo.
                ParseExpresionAritmetica();
            }
            else if (t == "PR16" || t == "PR17")
            {
                Match(TokenActual());
            }else if (t == "OL2")
            {
                ParseIN14();
            }
            else
            {
                throw new Exception($"Valor o expresión inválida en la asignación. No se reconoce: '{t}'");
            }
        }

        private void ParseExpresionAritmetica()
        {
            ParseTerminoAritmetico();

            // Si después del primer número o ID viene un operador (+, -, *, /)
            while (TokenActual() == "OA1" || TokenActual() == "OA2" || TokenActual() == "OA3" || TokenActual() == "OA4" || TokenActual() == "OA5")
            {
                Match(TokenActual()); // Consumimos el operador (+, -, *, /)
                ParseTerminoAritmetico();
            }
        }

        private void ParseTerminoAritmetico()
        {
            string t = TokenActual();
            if (t == "CNU" || t == "CN" || t.StartsWith("IDENT"))
            {
                Match(t); // Consume el número o la variable
            }
            else if (t == "ce07") // Paréntesis de apertura '('
            {
                Match("ce07");
                ParseExpresionAritmetica(); // Permite sub-expresiones como (a + b)
                Match("ce08"); // Paréntesis de cierre ')'
            }
            else
            {
                throw new Exception("Se esperaba un número, variable o '('");
            }
        }

        private void ParseValorCadena()
        {
            Match(TokenActual()); // Consume la constante de cadena (CDE)

            if (TokenActual() == "OA1") // Ajusta a tu token de suma
            {
                Match(TokenActual());
                ParseValorCadena(); // Recursividad para seguir concatenando
            }
        }

        private void ParseINCRE()
        { // ID OPA OPAR
            Match("ID"); Match("opa"); ParseOPAR();
        }

        private void ParseOPAR()
        {
            ParseValorAritmetico();
            //while (TokenActual() == "OAR" || TokenActual() == "OPA+" || TokenActual() == "OPA-" || TokenActual() == "OPA*" || TokenActual() == "OPA/")
            while (TokenActual() == "OA5" || TokenActual() == "OA1" || TokenActual() == "OA2" || TokenActual() == "OA3" || TokenActual() == "OA4")
            {
                Match(TokenActual());
                ParseValorAritmetico();
            }
        }

        private void ParseValorAritmetico()
        {
            if (TokenActual().StartsWith("IDENT")) Match("ID");
            else if (TokenActual() == "CNU" || TokenActual() == "CN") Match(TokenActual());
            else throw new Exception($"Se esperaba ID o Número, se encontró '{TokenActual()}'");
        }

        private void ParseCONDIC()
        {
            ParseValorCondicion();
            while (TokenActual() == "OPR" || TokenActual() == "OPL" || TokenActual().StartsWith("OR") || TokenActual().StartsWith("OL"))
            {
                Match(TokenActual());
                ParseValorCondicion();
            }
        }

        private void ParseValorCondicion()
        {
            string t = TokenActual();
            if (t.StartsWith("IDENT")) Match("ID");
            else if (t == "CNU" || t == "CN" || t == "CAD" || t == "PR16" || t == "PR17") Match(t);
            else if (t == "OL2") ParseIN14();
            else throw new Exception($"Valor de condición no válido: {t}");
        }

        #endregion


        private void btnLexico_Click(object sender, EventArgs e)
        {
            ActualizarErrores();
            ActualizarSimbolos();
            try
            {
                listaErrores.Clear();
                listaSimbolos.Clear();
                if (string.IsNullOrEmpty(rtxtCodigo.Text))
                    throw new Exception("No hay código");

                string[] lineasCodigo = rtxtCodigo.Lines;
                contadorSimbolos = 1;
                int estadoActual = 1;
                char car;
                string strCar = "";
                string celdaActual = "";
                List<string> filasDeTokens = new List<string>();
                string tokens = "";
                bool cadAbierta = false;
                bool comentario = false, tokenFDLSeguidoCar = false;
                int j = 0;
                contE = 0;
                string val = "";
                string  valCadena = "";
                for (int numLinea = 0; numLinea < lineasCodigo.Length; numLinea++)
                {
                    string lineaCodActual = lineasCodigo[numLinea];
                    string tokensDeLinea = "";
                    string lineaLista = lineaCodActual.TrimEnd();
                    string tokenFDL = "";

                    bool enComillas = false;
                    int indiceComentario = -1;
                    for (int i = 0; i < lineaCodActual.Length; i++)
                    {
                        if (lineaCodActual[i] == '"') enComillas = !enComillas;
                        else if (lineaCodActual[i] == '#' && !enComillas)
                        {
                            indiceComentario = i;
                            break;
                        }
                    }

                    bool tieneComentario = false;
                    if (indiceComentario >= 0)
                    {
                        lineaCodActual = lineaCodActual.Substring(0, indiceComentario);
                        tieneComentario = true; // Guardamos bandera para imprimir COM después
                    }

                    lineaLista = lineaCodActual.TrimEnd();
                    string lineaLimpia = lineaLista.Trim();
                     tokenFDL = "";

                    // Si la línea era solo un comentario, ahora estará vacía. La saltamos.
                    if (string.IsNullOrEmpty(lineaLimpia))
                    {
                        if (tieneComentario)
                        {
                            filasDeTokens.Add("COM"); // Imprimimos el token COM solitario
                        }
                        else
                        {
                            filasDeTokens.Add("");
                        }
                        continue;
                    }

                    // Validar si la línea contiene únicamente palabras de control
                    if (lineaLimpia == "INI" || lineaLimpia == "FIN" || lineaLista.EndsWith("{") || lineaLista.EndsWith("}") || lineaLista.EndsWith(":"))
                    {
                        tokenFDL = "";
                    }
                    else if (!lineaLista.EndsWith("~"))
                    {
                        tokenFDL = "ERRFDL ";
                        contE++;
                        RegistrarError(numLinea + 1, "Falta el símbolo delimitador '~' al final");
                    }
                    else
                    {
                        tokenFDL = "FDL ";
                    }
                    lineaCodActual = lineaLista+" ";
                    cadAbierta = false;
                    comentario = false;

                    for (int numCaracter = 0; numCaracter < lineaCodActual.Length; numCaracter++)
                    {
                        if (lineaCodActual[numCaracter] == '~' && numCaracter < lineaCodActual.Length - 2)
                        {
                            if (numCaracter > 0 && lineaCodActual[numCaracter - 1] != ' ')
                            {
                                tokenFDLSeguidoCar = true;

                            }
                            else
                            {
                                tokenFDLSeguidoCar = false;
                                tokensDeLinea += "FDL ";
                            }
                            numCaracter++;
                        }
                        else if (lineaCodActual[numCaracter] == '~' && lineaCodActual.Length-2 == numCaracter)
                        {
                            tokenFDL = "FDL ";
                            numCaracter++;
                        }
                        car = lineaCodActual[numCaracter];
                        if (estadoActual == 1 && (car == ' ' || car == '\t' || car == '\r' || car == '\n'))
                        {
                            continue; 
                        }
                        if (estadoActual == 1 && (car == '(' || car == ')' || car == '{' || car == '}' || car == ':' || car == ';'))
                        {
                            string tokenEspecial = "";
                            if (car == '(') tokenEspecial = "ce07";
                            else if (car == ')') tokenEspecial = "ce08";
                            else if (car == '{') tokenEspecial = "ce09";
                            else if (car == '}') tokenEspecial = "ce10";
                            else if (car == ':') tokenEspecial = "ce11";
                            else if (car == ';') tokenEspecial = "ce18";

                            tokensDeLinea += tokenEspecial + " ";
                            tokens += tokenEspecial + " ";
                            continue;
                        }
                        if (car == '"' && !comentario)
                        {
                            cadAbierta = !cadAbierta;
                        }else if(car == '#')
                        {
                            comentario = true;
                        }
                        if (!char.IsWhiteSpace(car) || cadAbierta)
                        {
                            valCadena += car;
                        }


                        if (numCaracter == lineaCodActual.Length - 1)
                        {
                            strCar = "FDC";
                        }
                        else if ((car == ' ' || car == '\t') && !cadAbierta && !comentario)
                        {
                            strCar = "FDC";
                        }
                        else if ((car == ' ' || car == '\t') && (cadAbierta || comentario))
                        {
                            
                            continue;
                        }
                        else
                        {
                            if (char.IsLetter(car))
                            {
                                strCar = car.ToString();
                                if (char.IsLower(car)) { strCar = car + "1"; }
                            }
                            else if (char.IsDigit(car))
                            {
                                strCar = "_" + car;
                            }
                            else
                            {
                                strCar = "c" + (int)car;
                            }
                        }
                        if (!matrizTransicion[estadoActual].ContainsKey(strCar))
                        {
                            throw new Exception("Símbolo fuera de la matriz de transición");
                        }
                        celdaActual = matrizTransicion[estadoActual][strCar];
                        if(celdaActual != "ERROR" && celdaActual != "ACEPTA")
                        {
                            if (matrizTransicion[int.Parse(celdaActual)]["FDC"] == "ERROR")
                            {
                                string token = matrizTransicion[int.Parse(celdaActual)]["CAT"];
                                tokensDeLinea += "[ERROR:" + token + "] " ;
                                tokens += "[ERROR:" + token + "] ";
                                RegistrarError(numLinea + 1, token);
                                contE++;
                                valCadena = "";
                                estadoActual = 1;
                                //contadorSimbolos--;

                            }
                            else if (matrizTransicion[int.Parse(celdaActual)]["FDC"] == "ACEPTA")
                            {
                                if (matrizTransicion[int.Parse(celdaActual)]["CAT"] == "PR24")
                                {

                                }
                                if (matrizTransicion[int.Parse(celdaActual)]["CAT"] == "IDVAL")
                                {
                                    RegistrarSimbolo(contadorSimbolos++, valCadena);
                                    Simbolo simboloEncontrado = listaSimbolos.FirstOrDefault(s => s.Nombre == valCadena);
                                    
                                    if (simboloEncontrado != null)
                                    {
                                        string idPersonalizado = "IDENT" + simboloEncontrado.Num;
                                        tokensDeLinea += idPersonalizado + " ";
                                        tokens += idPersonalizado + " ";
                                    }
                                }
                                else
                                {
                                    tokensDeLinea += matrizTransicion[int.Parse(celdaActual)]["CAT"] + " ";
                                    tokens += matrizTransicion[int.Parse(celdaActual)]["CAT"] + " ";
                                }
                                estadoActual = 1;
                                val = matrizTransicion[int.Parse(celdaActual)]["CAT"] + " ";
                                valCadena = "";

                                if (strCar != "FDC")
                                {
                                    numCaracter--;
                                }
                            }
                            else
                            {
                                estadoActual = int.Parse(celdaActual);
                            }
                            if (tokenFDLSeguidoCar)
                            {
                                tokensDeLinea += "FDL ";
                                tokenFDLSeguidoCar = false;
                            }
                        }
                        
                    }
                    if (cadAbierta)
                    {
                        tokensDeLinea += "[ERROR:ECADINV] ";
                        tokens += "[ERROR:ECADINV] ";
                        RegistrarError(numLinea + 1, "ECADINV");
                        contE++;
                        cadAbierta = false;
                        estadoActual = 1;
                        valCadena = "";
                    }
                    tokens.TrimEnd(' ');
                    tokens += "\n";
                    tokensDeLinea += tokenFDL;
                    if (tieneComentario)
                    {
                        tokensDeLinea += "COM";
                    }

                    comentario = false;
                    filasDeTokens.Add(tokensDeLinea.TrimEnd());
                }
                rtxtTokens.Lines = filasDeTokens.ToArray();
                ResaltarErrores(rtxtTokens);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message,"Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            ActualizarErrores();
            ActualizarSimbolos();
            lblCE.Text = "Total de errores: " + contE;
        }

        private void RegistrarError(int linea, string mensaje)
        {
            listaErrores.Add(new ErrorLexico
            {
                Linea = linea,
                Descripcion = mensaje
            });
            ActualizarErrores();
        }

        private void RegistrarSimbolo(int num, string nombre)
        {
            bool flag = true;
            foreach (Simbolo simbolo in listaSimbolos)
            {
                if (simbolo.Nombre == nombre)
                {
                    flag = false;
                    contadorSimbolos--;
                }
            }
            if(flag)
            {
                listaSimbolos.Add(new Simbolo
                {
                    Num = num,
                    Nombre = nombre
                });
                ActualizarSimbolos();
            }
            
        }

        private void ActualizarErrores()
        {
            DgvErrores.Rows.Clear();
            foreach (ErrorLexico error in listaErrores)
            {
                switch (error.Descripcion)
                {
                    case "PRINV":
                        error.Descripcion = "Palabra Reservada INVALIDA";
                        break;
                    case "CNINV":
                        error.Descripcion = "Constante Numerica INVALIDA";

                        break;
                    case "VAINV":
                        error.Descripcion = "Valor INVALIDO";

                        break;
                    case "IDINV":
                        error.Descripcion = "Identificador INVALIDO";

                        break;
                    case "EOPARINV":
                        error.Descripcion = "Error aritmetico";

                        break;
                    case "EOPRELINV":
                        error.Descripcion = "Error operador relacion";

                        break;
                    case "ECADINV":
                        error.Descripcion = "Error cadena INVALIDA";

                        break;
                    case "ECARINV":
                        error.Descripcion = "Error caracter INVALIDO";

                        break;
                }
               DgvErrores.Rows.Add(error.Linea, error.Descripcion);
            }
        }
        private void ActualizarSimbolos()
        {
            DgvSimbolos.Rows.Clear();
            foreach (Simbolo simbolo in listaSimbolos)
            {
                DgvSimbolos.Rows.Add(simbolo.Num, simbolo.Nombre);
            }

        }

        private void rtxtCodigo_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.KeyCode == Keys.Tab)
            {
                e.SuppressKeyPress = true;
                rtxtCodigo.SelectedText = "     ";
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void ActualizarNumerosDeLinea()
        {
            
        }

        private void btnCargarProg_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Archivos de texto (*.txt)|*.txt|Archivos RTF (*.rtf)|*.rtf|Todos los archivos (*.*)|*.*";
                openFileDialog.Title = "Selecciona un archivo para cargar";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        rtxtCodigo.Enabled = false;
                        string rutaArchivo = openFileDialog.FileName;

                        // Comprobamos la extensión para saber cómo cargarlo
                        if (Path.GetExtension(rutaArchivo).ToLower() == ".rtf")
                        {
                            // Si es un archivo con formato RTF, usamos el método nativo LoadFile
                            rtxtCodigo.LoadFile(rutaArchivo, RichTextBoxStreamType.RichText);
                        }
                        else
                        {
                            // Si es texto plano (.txt, .csv, etc.), leemos todo el texto y lo asignamos
                            rtxtCodigo.Text = File.ReadAllText(rutaArchivo);

                            // Alternativamente, puedes usar: 
                            // richTextBox1.LoadFile(rutaArchivo, RichTextBoxStreamType.PlainText);
                        }
                        ActualizarNumerosDeLinea();
                    }
                    catch (Exception ex)
                    {
                        // Mostramos un mensaje si ocurre algún error (ej. archivo bloqueado)
                        rtxtCodigo.Enabled = true;
                        MessageBox.Show("Ocurrió un error al cargar el archivo: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void btnGuardarProg_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                // Configuramos los filtros (por defecto seleccionará .txt)
                saveFileDialog.FileName = "Tokens.txt";
                saveFileDialog.Filter = "Archivos de texto (*.txt)|*.txt|Archivos RTF (*.rtf)|*.rtf|Todos los archivos (*.*)|*.*";
                saveFileDialog.Title = "Guardar contenido como...";
                saveFileDialog.DefaultExt = "txt"; // Extensión por defecto si el usuario no escribe una

                // Si el usuario elige dónde guardar y hace clic en "Guardar"
                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        string rutaArchivo = saveFileDialog.FileName;

                        // Verificamos si el usuario decidió guardar como RTF (con formato)
                        if (Path.GetExtension(rutaArchivo).ToLower() == ".rtf")
                        {
                            // Guarda conservando colores, negritas, etc.
                            rtxtCodigo.SaveFile(rutaArchivo, RichTextBoxStreamType.RichText);
                        }
                        else
                        {
                            // GUARDA COMO TEXTO PLANO
                            // La propiedad .Text contiene el texto tal cual, con sus saltos de línea nativos.
                            File.WriteAllText(rutaArchivo, rtxtCodigo.Text);

                            // Nota: Otra forma igualmente válida de hacerlo es:
                            // richTextBox1.SaveFile(rutaArchivo, RichTextBoxStreamType.PlainText);
                        }

                        // Opcional: Mostrar un mensaje de éxito
                        MessageBox.Show("El archivo se guardó correctamente.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        // Manejo de errores por si no hay permisos de escritura en esa carpeta, etc.
                        MessageBox.Show("Ocurrió un error al guardar el archivo: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void btnGuardarArchTokens_Click(object sender, EventArgs e)
        {
            if (contE > 0)
            {
                MessageBox.Show("No se puede guardar ya que hay errores", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                // Configuramos los filtros (por defecto seleccionará .txt)
                saveFileDialog.Filter = "Archivos de texto (*.txt)|*.txt|Archivos RTF (*.rtf)|*.rtf|Todos los archivos (*.*)|*.*";
                saveFileDialog.Title = "Guardar contenido como...";
                saveFileDialog.DefaultExt = "txt"; // Extensión por defecto si el usuario no escribe una

                // Si el usuario elige dónde guardar y hace clic en "Guardar"
                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        string rutaArchivo = saveFileDialog.FileName;

                        // Verificamos si el usuario decidió guardar como RTF (con formato)
                        if (Path.GetExtension(rutaArchivo).ToLower() == ".rtf")
                        {
                            // Guarda conservando colores, negritas, etc.
                            rtxtTokens.SaveFile(rutaArchivo, RichTextBoxStreamType.RichText);
                        }
                        else
                        {
                            // GUARDA COMO TEXTO PLANO
                            // La propiedad .Text contiene el texto tal cual, con sus saltos de línea nativos.
                            File.WriteAllText(rutaArchivo, rtxtTokens.Text);

                            // Nota: Otra forma igualmente válida de hacerlo es:
                            // richTextBox1.SaveFile(rutaArchivo, RichTextBoxStreamType.PlainText);
                        }

                        // Opcional: Mostrar un mensaje de éxito
                        MessageBox.Show("El archivo se guardó correctamente.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        // Manejo de errores por si no hay permisos de escritura en esa carpeta, etc.
                        MessageBox.Show("Ocurrió un error al guardar el archivo: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void btnEditarProg_Click(object sender, EventArgs e)
        {
            rtxtCodigo.Enabled = true;
        }
        private void ResaltarErrores(RichTextBox rtxt)
        {
            string[] palabrasClave = { "[ERROR:VAINV]","[ERROR:IDINV]","[ERROR:CNINV]","[ERROR:PRINV]","[ERROR:EOPARINV]","[ERROR:EOPRELINV]","[ERROR:ECADINV]","[ERROR:ECARINV]","ERRFDL"};

            foreach (string palabra in palabrasClave)
            {
                int startindex = 0;
                while (startindex < rtxt.TextLength)
                {
                    // Busca la palabra a partir del último índice encontrado
                    int wordinterv = rtxt.Find(palabra, startindex, RichTextBoxFinds.None);

                    if (wordinterv != -1)
                    {
                        rtxt.Select(wordinterv, palabra.Length);
                        rtxt.SelectionColor = Color.Red; // Cambia a rojo
                        startindex = wordinterv + palabra.Length;
                    }
                    else
                    {
                        break;
                    }
                }
            }
            // Quitar la selección al terminar para que no quede texto marcado
            rtxt.SelectionStart = rtxt.Text.Length;
            rtxt.SelectionLength = 0;
            rtxt.SelectionColor = Color.Black;
        }

        private void rtxtCodigo_TextChanged(object sender, EventArgs e)
        {
            Point pt = new Point(0, 0);
            int firstIndex = rtxtCodigo.GetCharIndexFromPosition(pt);
            int firstLine = rtxtCodigo.GetLineFromCharIndex(firstIndex);

            pt.X = rtxtCodigo.ClientRectangle.Width;
            pt.Y = rtxtCodigo.ClientRectangle.Height;
            int lastIndex = rtxtCodigo.GetCharIndexFromPosition(pt);
            int lastLine = rtxtCodigo.GetLineFromCharIndex(lastIndex);

            rtxLineasCodigo.SelectionAlignment = HorizontalAlignment.Center;
            rtxLineasCodigo.Text = "";
            rtxTokens.SelectionAlignment = HorizontalAlignment.Center;
            rtxTokens.Text = "";

            for (int i = firstLine; i< lastLine + 1; i++)
            {
                rtxLineasCodigo.Text += (i + 1) + "\n";
                rtxTokens.Text += (i + 1) + "\n";
            }
        }

        private void rtxtCodigo_VScroll(object sender, EventArgs e)
        {
            Point pt = new Point(0, 0);
            int firstIndex = rtxtCodigo.GetCharIndexFromPosition(pt);
            int firstLine = rtxtCodigo.GetLineFromCharIndex(firstIndex);

            pt.X = rtxtCodigo.ClientRectangle.Width;
            pt.Y = rtxtCodigo.ClientRectangle.Height;
            int lastIndex = rtxtCodigo.GetCharIndexFromPosition(pt);
            int lastLine = rtxtCodigo.GetLineFromCharIndex(lastIndex);

            rtxLineasCodigo.SelectionAlignment = HorizontalAlignment.Center;
            rtxLineasCodigo.Text = "";
            rtxTokens.SelectionAlignment = HorizontalAlignment.Center;
            rtxTokens.Text = "";

            for (int i = firstLine; i < lastLine + 1; i++)
            {
                rtxLineasCodigo.Text += (i + 1) + "\n";
                rtxTokens.Text += (i + 1) + "\n";
            }
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
        }

        private void tabControl1_KeyDown(object sender, KeyEventArgs e)
        {
        }

        private async Task MostrarTrazaAnimada()
        {
            rtxtAnSintSINO.Clear();
            foreach (string paso in pasosTraza)
            {
                rtxtAnSintSINO.AppendText(paso + "\n");
                // Hacemos que el RichTextBox haga scroll automático hacia abajo
                rtxtAnSintSINO.SelectionStart = rtxtAnSintSINO.Text.Length;
                rtxtAnSintSINO.ScrollToCaret();

                // Pausa de 2000 milisegundos (2 segundos) sin trabar el programa
                await Task.Delay(2000);
            }
        }

        private async void btnAnalizadorSintactico_Click(object sender, EventArgs e)
        {
            if (contE > 0)
            {
                MessageBox.Show("No se puede iniciar el Análisis Sintáctico. El código contiene Errores Léxicos que deben corregirse primero.", "Fase Léxica Incompleta", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(rtxtTokens.Text))
            {
                MessageBox.Show("Primero debe ejecutar el Análisis Léxico para generar los tokens.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            // 1. Limpiamos interfaz y estructuras
            dtgErrsSint.Rows.Clear();
            rtxtAnSint.Clear(); // Limpiamos el RichTextBox de la traza
            listaErroresSintacticos.Clear();
            traduccionSintactica.Clear();
            tokensSintacticosObj.Clear();
            punteroSintactico = 0;
            rtxtAnSintSINO.Clear();
            pasosTraza.Clear();

            // 2. Extraemos los tokens línea por línea manteniendo el control de en qué línea estaban
            string[] lineas = rtxtTokens.Lines;
            for (int i = 0; i < lineas.Length; i++)
            {
                string lineaLimpia = lineas[i].Trim();
                if (string.IsNullOrEmpty(lineaLimpia)) continue;

                string[] toks = lineaLimpia.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

                // Normalizaciones solicitadas (manteniendo tus tokens del lexer original)
                foreach (var t in toks)
                {
                    if (t == "COM") continue;
                    string tokenNormalizado = t;
                    if (tokenNormalizado == "ce7") tokenNormalizado = "ce07";
                    if (tokenNormalizado == "ce8") tokenNormalizado = "ce08";
                    if (tokenNormalizado == "ce9") tokenNormalizado = "ce09";

                    tokensSintacticosObj.Add(new TokenSintactico { Valor = tokenNormalizado, Linea = i + 1 });
                }
            }

            // 3. Iniciar evaluación Top-Down
            try
            {
                ParsePrograma();

                // 4. Llenamos el RichTextBox con la traducción de las reglas en ejecución
                rtxtAnSint.Text = traduccionSintactica.ToString();
                rtxtAnSint.Text = traduccionSintactica.ToString();
                rtxtAnSintSINO.Text = trazaTopDown.ToString();

                // 5. Validamos si el arreglo de Errores recolectados está limpio o tiene datos
                if (listaErroresSintacticos.Count > 0)
                {
                    foreach (var err in listaErroresSintacticos)
                    {
                        dtgErrsSint.Rows.Add(err.Linea, err.Descripcion);
                    }
                    MessageBox.Show($"El análisis finalizó, pero se encontraron {listaErroresSintacticos.Count} errores sintácticos.", "Errores Encontrados", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                else
                {
                    MessageBox.Show("¡Análisis Sintáctico Exitoso!\nEl código cumple con todas las reglas gramaticales.", "Sintaxis Correcta", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    await MostrarTrazaAnimada();
                }
            }
            catch (Exception ex)
            {
                // En caso de un fallo catastrófico (EOF inesperado sin punto de recuperación)
                dtgErrsSint.Rows.Add(LineaActual(), ex.Message);
                MessageBox.Show("El analizador se detuvo por un error estructural no recuperable.", "Fallo Crítico", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void lblEquipo_Click(object sender, EventArgs e)
        {

        }
    }
}
