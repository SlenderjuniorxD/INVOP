using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace INVOP.Logica
{
    public class MSolver
    {
        // Definimos M como un número muy grande, tal como acordamos (100 o 1000 es suficiente para ejercicios pequeños, 1M para software)
        private const double M = 100000;
        private const int MAX_ITERACIONES = 100; // Para evitar bucles infinitos

        public class ResultadoSimplex
        {
            public List<DatosIteracion> Historial { get; set; } = new List<DatosIteracion>(); // <--- NUEVO
            public List<string> Pasos { get; set; } = new List<string>(); // Bitácora de lo que pasó
            public double ValorZ { get; set; }
            public double[] VariablesDecision { get; set; }
            public bool EsFactible { get; set; }
            public string MensajeError { get; set; }
        }
        public ResultadoSimplex Resolver(List<double> funcionZ, List<(List<double> coefs, string signo, double rhs)> restricciones, bool esMinimizar)
        {
            var resultado = new ResultadoSimplex();
            int numVarsOriginales = funcionZ.Count;
            int numRestricciones = restricciones.Count;

            // ---------------------------------------------------------
            // PASO 1: CONFIGURACIÓN DE COLUMNAS Y VARIABLES
            // ---------------------------------------------------------
            List<string> nombresCols = new List<string>();

            // 1. Nombres variables originales (x1, x2...)
            for (int i = 0; i < numVarsOriginales; i++)
                nombresCols.Add($"x{i + 1}");

            // 2. Calcular columnas adicionales y generar nombres continuos (x3, x4...)
            int contadorVars = numVarsOriginales + 1;
            int numHolguras = 0;
            int numExcesos = 0;
            int numArtificiales = 0;

            // Listas para saber qué columna corresponde a qué tipo
            List<int> colsArtificiales = new List<int>();
            List<int> indicesBasicas = new List<int>(); // Guardará el índice de columna de la variable básica de cada fila

            foreach (var r in restricciones)
            {
                if (r.signo == "<=")
                {
                    nombresCols.Add($"x{contadorVars}"); // Holgura
                    indicesBasicas.Add(nombresCols.Count - 1); // Esta es la básica
                    numHolguras++;
                    contadorVars++;
                }
                else if (r.signo == ">=")
                {
                    nombresCols.Add($"x{contadorVars}"); // Exceso
                    numExcesos++;
                    contadorVars++;

                    nombresCols.Add($"x{contadorVars}"); // Artificial
                    colsArtificiales.Add(nombresCols.Count - 1);
                    indicesBasicas.Add(nombresCols.Count - 1); // Esta es la básica
                    numArtificiales++;
                    contadorVars++;
                }
                else if (r.signo == "=")
                {
                    nombresCols.Add($"x{contadorVars}"); // Artificial
                    colsArtificiales.Add(nombresCols.Count - 1);
                    indicesBasicas.Add(nombresCols.Count - 1); // Esta es la básica
                    numArtificiales++;
                    contadorVars++;
                }
            }
            nombresCols.Add("Sol."); // Columna final

            // ---------------------------------------------------------
            // PASO 2: CONSTRUCCIÓN DE LA MATRIZ INICIAL
            // ---------------------------------------------------------
            int totalColumnas = nombresCols.Count - 1; // Sin contar la etiqueta "Sol."
            double[,] tabla = new double[numRestricciones + 1, totalColumnas + 1];

            // A) Llenar Restricciones
            int colActual = numVarsOriginales;

            for (int i = 0; i < numRestricciones; i++)
            {
                // Coeficientes originales
                for (int j = 0; j < numVarsOriginales; j++)
                    tabla[i + 1, j] = restricciones[i].coefs[j];

                // Variables extra
                if (restricciones[i].signo == "<=")
                {
                    tabla[i + 1, colActual] = 1; // Holgura
                    colActual++;
                }
                else if (restricciones[i].signo == ">=")
                {
                    tabla[i + 1, colActual] = -1; // Exceso
                    colActual++;
                    tabla[i + 1, colActual] = 1;  // Artificial
                    colActual++;
                }
                else if (restricciones[i].signo == "=")
                {
                    tabla[i + 1, colActual] = 1;  // Artificial
                    colActual++;
                }

                // Lado Derecho (RHS)
                tabla[i + 1, totalColumnas] = restricciones[i].rhs;
            }

            // B) Llenar Fila Z Inicial (Coeficientes originales negativos)
            for (int j = 0; j < numVarsOriginales; j++)
            {
                tabla[0, j] = -funcionZ[j];
            }

            // C) Penalización M en la Tabla
            // Si MINIMIZAR: Z = ... + M*Art -> Pasa a la izquierda como -M
            // Si MAXIMIZAR: Z = ... - M*Art -> Pasa a la izquierda como +M
            double penalizacionEnTabla = esMinimizar ? -M : M;

            foreach (int colArt in colsArtificiales)
            {
                tabla[0, colArt] = penalizacionEnTabla;
            }

            // ---------------------------------------------------------
            // PASO 3: LIMPIEZA DE LA FILA Z (Eliminar M de las básicas)
            // ---------------------------------------------------------
            foreach (int colArt in colsArtificiales)
            {
                // Buscar en qué fila está esta artificial
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
                    // Operación: Z_nueva = Z_vieja - (valorEnZ * Fila_Artificial)
                    for (int j = 0; j <= totalColumnas; j++)
                    {
                        tabla[0, j] = tabla[0, j] - (valorEnZ * tabla[fila, j]);
                    }
                }
            }

            // Guardar estado inicial (Iteración 0)
            GuardarIteracion(resultado, tabla, 0, indicesBasicas, nombresCols, -1, -1);

            // ---------------------------------------------------------
            // PASO 4: BUCLE SIMPLEX (Iteraciones)
            // ---------------------------------------------------------
            int iteracion = 0;
            while (iteracion < MAX_ITERACIONES)
            {
                // 1. Buscar Columna Pivote
                int colPivote = -1;
                double mejorValor = 0;

                for (int j = 0; j < totalColumnas; j++)
                {
                    double val = tabla[0, j];
                    if (esMinimizar)
                    {
                        // Minimizar: Buscamos el más POSITIVO
                        if (val > 0.00001 && val > mejorValor) { mejorValor = val; colPivote = j; }
                    }
                    else
                    {
                        // Maximizar: Buscamos el más NEGATIVO
                        if (val < -0.00001 && val < mejorValor) { mejorValor = val; colPivote = j; }
                    }
                }

                if (colPivote == -1) break; // Óptimo encontrado

                // 2. Buscar Fila Pivote (Ratio Mínimo Positivo)
                int filaPivote = -1;
                double menorRatio = double.MaxValue;

                for (int i = 1; i <= numRestricciones; i++)
                {
                    double coef = tabla[i, colPivote];
                    double rhs = tabla[i, totalColumnas];

                    if (coef > 0.00001)
                    {
                        double ratio = rhs / coef;
                        if (ratio < menorRatio)
                        {
                            menorRatio = ratio;
                            filaPivote = i;
                        }
                    }
                }

                if (filaPivote == -1)
                {
                    resultado.MensajeError = "Solución no acotada.";
                    return resultado;
                }

                // Actualizamos la foto anterior con los datos del pivote encontrado
                var ultimaFoto = resultado.Historial.Last();
                ultimaFoto.FilaPivote = filaPivote;
                ultimaFoto.ColumnaPivote = colPivote;

                // 3. Pivoteo (Gauss-Jordan)
                double elementoPivote = tabla[filaPivote, colPivote];

                // A) Normalizar fila pivote
                for (int j = 0; j <= totalColumnas; j++)
                    tabla[filaPivote, j] /= elementoPivote;

                // Actualizar básica
                indicesBasicas[filaPivote - 1] = colPivote;

                // B) Hacer ceros
                for (int i = 0; i <= numRestricciones; i++)
                {
                    if (i != filaPivote)
                    {
                        double factor = tabla[i, colPivote];
                        for (int j = 0; j <= totalColumnas; j++)
                            tabla[i, j] -= factor * tabla[filaPivote, j];
                    }
                }

                iteracion++;
                // Guardar estado actual
                GuardarIteracion(resultado, tabla, iteracion, indicesBasicas, nombresCols, -1, -1);
            }

            // ---------------------------------------------------------
            // PASO 5: RESULTADOS FINALES
            // ---------------------------------------------------------
            double[] valoresFinales = new double[numVarsOriginales];
            for (int i = 0; i < numVarsOriginales; i++)
            {
                int fila = -1;
                for (int r = 0; r < indicesBasicas.Count; r++)
                {
                    if (indicesBasicas[r] == i) { fila = r + 1; break; }
                }
                valoresFinales[i] = fila != -1 ? tabla[fila, totalColumnas] : 0;
            }

            resultado.VariablesDecision = valoresFinales;

            // Recalcular Z real para evitar errores de signo en la tabla
            double zFinal = 0;
            for (int i = 0; i < numVarsOriginales; i++)
                zFinal += funcionZ[i] * valoresFinales[i];

            resultado.ValorZ = zFinal;
            resultado.EsFactible = true;

            return resultado;
        }

        // Método auxiliar para clonar la matriz y guardarla
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
        public double[,] Matriz { get; set; } // La tabla completa
        public List<string> NombresColumnas { get; set; } // x1, x2, s1, a1...
        public List<string> NombresBasicas { get; set; } // Quién está en la base (filas)
        public int FilaPivote { get; set; } = -1; // Para pintarla de color
        public int ColumnaPivote { get; set; } = -1; // Para pintarla de color
    }
}
