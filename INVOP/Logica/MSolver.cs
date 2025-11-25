using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace INVOP.Logica
{
    public class MSolver
    {

        private const double M = 100000;
        private const int MAX_ITERACIONES = 100;

        public class ResultadoSimplex
        {
            public List<DatosIteracion> Historial { get; set; } = new List<DatosIteracion>();
            public double ValorZ { get; set; }
            public double[] VariablesDecision { get; set; }
            public bool EsFactible { get; set; }
            public string MensajeError { get; set; }
        }
        public ResultadoSimplex Resolver(List<double> funcionZ, List<(List<double> coefs, string signo, double ld)> restricciones, bool esMinimizar)
        {
            var resultado = new ResultadoSimplex();
            int numVarsOriginales = funcionZ.Count;
            int numRestricciones = restricciones.Count;

            List<string> nombresCols = new List<string>();

            for (int i = 0; i < numVarsOriginales; i++)
            {
                nombresCols.Add($"x{i + 1}");
            }

            //calcular columnas adicionales
            int contadorVars = numVarsOriginales + 1;
            int numHolguras = 0;
            int numExcesos = 0;
            int numArtificiales = 0;

            //listas para saber qué columna corresponde a que tipo
            List<int> colsArtificiales = new List<int>();
            List<int> indicesBasicas = new List<int>();

            foreach (var r in restricciones)
            {
                if (r.signo == "<=")
                {
                    //holgura
                    nombresCols.Add($"x{contadorVars}");
                    indicesBasicas.Add(nombresCols.Count - 1);
                    numHolguras++;
                    contadorVars++;
                }
                else if (r.signo == ">=")
                {
                    //exceso
                    nombresCols.Add($"x{contadorVars}");
                    numExcesos++;
                    contadorVars++;

                    nombresCols.Add($"x{contadorVars}");
                    colsArtificiales.Add(nombresCols.Count - 1);
                    indicesBasicas.Add(nombresCols.Count - 1);
                    numArtificiales++;
                    contadorVars++;
                }
                else if (r.signo == "=")
                {
                    nombresCols.Add($"x{contadorVars}");
                    colsArtificiales.Add(nombresCols.Count - 1);
                    indicesBasicas.Add(nombresCols.Count - 1);
                    numArtificiales++;
                    contadorVars++;
                }
            }
            nombresCols.Add("Sol.");

            //matriz incial
            int totalColumnas = nombresCols.Count - 1;
            double[,] tabla = new double[numRestricciones + 1, totalColumnas + 1];

            //llenamos restricciones
            int colActual = numVarsOriginales;

            for (int i = 0; i < numRestricciones; i++)
            {
                // Coeficientes originales
                for (int j = 0; j < numVarsOriginales; j++)
                    tabla[i + 1, j] = restricciones[i].coefs[j];

                // Variables extra
                if (restricciones[i].signo == "<=")
                {
                    tabla[i + 1, colActual] = 1; //holgura
                    colActual++;
                }
                else if (restricciones[i].signo == ">=")
                {
                    tabla[i + 1, colActual] = -1; //exceso
                    colActual++;
                    tabla[i + 1, colActual] = 1;  //artificial
                    colActual++;
                }
                else if (restricciones[i].signo == "=")
                {
                    tabla[i + 1, colActual] = 1;  //artificial
                    colActual++;
                }

                //lado Derecho
                tabla[i + 1, totalColumnas] = restricciones[i].ld;
            }

            //llenar fila z   inicial
            for (int j = 0; j < numVarsOriginales; j++)
            {
                tabla[0, j] = -funcionZ[j];
            }

            //penalizacion m en la Tabla

            double penalizacionEnTabla = esMinimizar ? -M : M;

            foreach (int colArt in colsArtificiales)
            {
                tabla[0, colArt] = penalizacionEnTabla;
            }

            //eliminar m
            foreach (int colArt in colsArtificiales)
            {
                //buscar en que fila esta esta artificial
                int fila = -1;

                for (int i = 1; i <= numRestricciones; i++)
                {
                    if (tabla[i, colArt] == 1)
                    {
                        fila = i;
                        break;
                    }
                }

                if (fila != -1)
                {
                    double valorEnZ = tabla[0, colArt];
                    
                    //nueva z
                    for (int j = 0; j <= totalColumnas; j++)
                    {
                        tabla[0, j] = tabla[0, j] - (valorEnZ * tabla[fila, j]);
                    }
                }
            }

            GuardarIteracion(resultado, tabla, 0, indicesBasicas, nombresCols, -1, -1);

            //metodo M aplicado
            //resolusion simplex


            int iteracion = 0;
            while (iteracion < MAX_ITERACIONES)
            {
                //columna Pivote
                int colPivote = -1;
                double mejorValor = 0;

                for (int j = 0; j < totalColumnas; j++)
                {
                    double val = tabla[0, j];
                    if (esMinimizar)
                    {
                        //el mas positivo
                        if (val > 0.00001 && val > mejorValor)
                        { 
                            mejorValor = val;
                            colPivote = j;
                        }
                    }
                    else
                    {
                        //max el mas negativo
                        if (val < -0.00001 && val < mejorValor)
                        { 
                            mejorValor = val;
                            colPivote = j; 
                        }
                    }
                }

                if (colPivote == -1) break; //optimo encontrado

                //fila Pivote

                int filaPivote = -1;
                double menor = double.MaxValue;

                for (int i = 1; i <= numRestricciones; i++)
                {
                    double coef = tabla[i, colPivote];
                    double rhs = tabla[i, totalColumnas];

                    if (coef > 0.00001)
                    {
                        double rango = rhs / coef;
                        if (rango < menor)
                        {
                            menor = rango;
                            filaPivote = i;
                        }
                    }
                }

                if (filaPivote == -1)
                {
                    resultado.MensajeError = "Solución no acotada";
                    return resultado;
                }

                //guardamos el pivote encontrado
                var ultimaIt = resultado.Historial.Last();
                ultimaIt.FilaPivote = filaPivote;
                ultimaIt.ColumnaPivote = colPivote;

                //pivoteo
                double elementoPivote = tabla[filaPivote, colPivote];

                //convertir en 1 fila pivote
                for (int j = 0; j <= totalColumnas; j++)
                {
                    tabla[filaPivote, j] /= elementoPivote;
                }

                //actualizar variables basicass
                indicesBasicas[filaPivote - 1] = colPivote;

                //hacer ceros arriba y abajoi
                for (int i = 0; i <= numRestricciones; i++)
                {
                    if (i != filaPivote)
                    {
                        double factor = tabla[i, colPivote];
                        for (int j = 0; j <= totalColumnas; j++)
                        {
                            tabla[i, j] -= factor * tabla[filaPivote, j];
                        }
                    }
                }

                iteracion++;

                //guardar estado actual
                GuardarIteracion(resultado, tabla, iteracion, indicesBasicas, nombresCols, -1, -1);
            }

            // resultado final
            double[] valoresFinales = new double[numVarsOriginales];

            for (int i = 0; i < numVarsOriginales; i++)
            {
                int fila = -1;
                for (int r = 0; r < indicesBasicas.Count; r++)
                {
                    if (indicesBasicas[r] == i)
                    { 
                        fila = r + 1;
                        break; 
                    }
                }
                valoresFinales[i] = fila != -1 ? tabla[fila, totalColumnas] : 0;
            }

            resultado.VariablesDecision = valoresFinales;

            //reemplazamos x1 y x2 en z
            double zFinal = 0;
            for (int i = 0; i < numVarsOriginales; i++)
            {
                zFinal += funcionZ[i] * valoresFinales[i];
            }
            
            resultado.ValorZ = zFinal;
            resultado.EsFactible = true;

            return resultado;
        }

        //metodo para guardar la matriz
        private void GuardarIteracion(ResultadoSimplex res, double[,] tabla, int iter, List<int> indicesBasicas, List<string> nombresCols, int fPiv, int cPiv)
        {
            int filas = tabla.GetLength(0);
            int cols = tabla.GetLength(1);
            double[,] copia = new double[filas, cols];
            Array.Copy(tabla, copia, tabla.Length);

            List<string> nombresBasicas = new List<string> { "Z" };
            foreach (int idx in indicesBasicas) nombresBasicas.Add(nombresCols[idx]);

            res.Historial.Add(new DatosIteracion
            {
                NumeroIteracion = iter,
                Matriz = copia,
                NombresColumnas = new List<string>(nombresCols),
                NombresBasicas = nombresBasicas,
                FilaPivote = fPiv,
                ColumnaPivote = cPiv
            });
        }
    }
    public class DatosIteracion
    {
        public int NumeroIteracion { get; set; }
        public double[,] Matriz { get; set; }
        public List<string> NombresColumnas { get; set; }
        public List<string> NombresBasicas { get; set; }
        public int FilaPivote { get; set; } = -1;
        public int ColumnaPivote { get; set; } = -1;
    }
}
