using Guna.UI2.WinForms;
using INVOP.Controles;
using INVOP.Logica;
using System.Globalization;
using static INVOP.Logica.Hungaro;
using static INVOP.Logica.MSolver;

namespace INVOP
{
    public partial class Form1 : Form
    {
        private int contadorVariables = 0;
        private Guna2TextBox[,] matrizTextBoxes;
        public Form1()
        {
            InitializeComponent();
            pnlRestricciones.SizeChanged += (sender, e) =>
            {
                pnlRestricciones.SuspendLayout();
                int nuevoAncho = pnlRestricciones.ClientSize.Width - 25;
                foreach (Control fila in pnlRestricciones.Controls)
                {
                    fila.Width = nuevoAncho;
                }
                pnlRestricciones.ResumeLayout();
            };
            tabPage1.SizeChanged += (s, e) =>
            {
                int nuevoAncho = tabPage1.ClientSize.Width - 40;
                pnlResultados.Width = nuevoAncho; // Ajustar panel contenedor

                // Ajustar cada tabla que ya esté dibujada
                foreach (Control c in pnlResultados.Controls)
                {
                    if (c is Guna2DataGridView grid)
                    {
                        grid.Width = nuevoAncho;
                    }
                }
            };
            numFilas.ValueChanged += DimensionesCambiadas;
            numColumnas.ValueChanged += DimensionesCambiadas;


            // Dibujamos la matriz inicial (3x3) al arrancar para que no salga vacío
            GenerarCuadricula((int)numFilas.Value, (int)numColumnas.Value);
        }

        private void btnAgregarVariable_Click(object sender, EventArgs e)
        {
            contadorVariables++;
            Guna2TextBox txtZ = new Guna2TextBox();
            txtZ.PlaceholderText = $"C{contadorVariables}";
            txtZ.Width = 60;
            pnlFuncionZ.Controls.Add(txtZ);

            // 2. Agregar cajita a TODAS las restricciones existentes
            foreach (Control c in pnlRestricciones.Controls)
            {
                if (c is FilaRestriccion fila)
                {
                    fila.AgregarVariable(contadorVariables);
                }
            }
        }

        private void btnAgregarRestriccion_Click(object sender, EventArgs e)
        {
            // Creamos una nueva instancia de nuestro control personalizado
            FilaRestriccion nuevaFila = new FilaRestriccion();
            if (pnlRestricciones.ClientSize.Width > 25)
            {
                nuevaFila.Width = pnlRestricciones.ClientSize.Width - 25;
            }
            else
            {
                nuevaFila.Width = 500; // Un ancho por defecto por si acaso
            }

            // Le agregamos tantas cajitas como variables existan actualmente
            for (int i = 1; i <= contadorVariables; i++)
            {
                nuevaFila.AgregarVariable(i);
            }

            // Lo añadimos al panel visual
            pnlRestricciones.Controls.Add(nuevaFila);
        }

        private void btnResolver_Click(object sender, EventArgs e)
        {

            try
            {
                // ---------------------------------------------------------
                // PASO 1: RECOPILAR LA FUNCIÓN OBJETIVO (Z)
                // ---------------------------------------------------------
                List<double> coeficientesZ = new List<double>();


                // Recorremos los TextBoxes del panel superior
                foreach (Control c in pnlFuncionZ.Controls)
                {
                    if (c is Guna2TextBox txt)
                    {
                        // Si está vacío, asumimos 0
                        double valor = 0;
                        if (!string.IsNullOrWhiteSpace(txt.Text))
                        {
                            if (!double.TryParse(txt.Text, out valor))
                            {
                                MessageBox.Show("Por favor, ingresa solo números válidos en la Función Z.");
                                return; // Detenemos todo si hay error
                            }
                        }
                        coeficientesZ.Add(valor);
                    }
                }

                // ---------------------------------------------------------
                // PASO 2: RECOPILAR LAS RESTRICCIONES
                // ---------------------------------------------------------
                // Preparamos la lista con el formato exacto que pide el Solver: (Coeficientes, Signo, RHS)
                var listaRestricciones = new List<(List<double>, string, double)>();

                foreach (Control c in pnlRestricciones.Controls)
                {
                    if (c is FilaRestriccion fila)
                    {
                        // Llamamos a tu método del UserControl que ya programaste
                        var datosFila = fila.ObtenerDatos();

                        // datosFila.Item1 es la lista de coeficientes (double)
                        // datosFila.Item2 es el signo (string)
                        // datosFila.Item3 es el resultado RHS (double)

                        // Validamos que la fila tenga la misma cantidad de variables que Z
                        if (datosFila.Item1.Count != coeficientesZ.Count)
                        {
                            // Si agregaste una variable a Z pero no se actualizó en una fila (raro, pero posible)
                            // Aquí podrías rellenar con ceros o lanzar error.
                            // Por ahora asumimos que tu lógica de "Agregar Variable" funcionó bien.
                        }

                        // Agregamos a la lista maestra
                        listaRestricciones.Add((datosFila.Item1, datosFila.Item2, datosFila.Item3));
                    }
                }

                // ---------------------------------------------------------
                // PASO 3: DETERMINAR SI ES MAXIMIZAR O MINIMIZAR
                // ---------------------------------------------------------
                // Asumo que tienes un RadioButton o CheckBox en tu formulario para esto
                // Por ejemplo: rbMinimizar.Checked
                bool esMinimizar = rbMinimizar.Checked; // ¡Cámbialo por tu control real! Ej: rbMinimizar.Checked;

                // ---------------------------------------------------------
                // PASO 4: LLAMAR AL CEREBRO MATEMÁTICO
                // ---------------------------------------------------------
                MSolver solver = new MSolver();

                // ¡AQUÍ OCURRE LA MAGIA! Enviamos las listas que acabamos de llenar
                var resultado = solver.Resolver(coeficientesZ, listaRestricciones, esMinimizar);

                // ---------------------------------------------------------
                // PASO 5: MOSTRAR RESULTADOS
                // ---------------------------------------------------------
                if (!resultado.EsFactible)
                {
                    MessageBox.Show("Error: " + resultado.MensajeError);
                }
                else
                {
                    MostrarIteraciones(resultado);
                    for (int i = 0; i < resultado.VariablesDecision.Length; i++)
                    {
                        // Redondeamos las variables también
                        double valorLimpio = Math.Round(resultado.VariablesDecision[i], 4);
                    }
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ocurrió un error inesperado: {ex.Message}");
            }
        }
        private void MostrarIteraciones(ResultadoSimplex resultado)
        {
            pnlResultados.Controls.Clear();
            int anchoTotal = 900;
            pnlResultados.Width = anchoTotal;
            Label separador = new Label
            {
                Text = "--- RESULTADOS ---",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.DimGray,
                AutoSize = true,
                Margin = new Padding(10, 20, 0, 10)
            };
            pnlResultados.Controls.Add(separador);

            foreach (var iteracion in resultado.Historial)
            {
                Label lblTitulo = new Label
                {
                    Text = $"Iteración {iteracion.NumeroIteracion}",
                    Font = new Font("Segoe UI", 10, FontStyle.Bold),
                    ForeColor = Color.Black,
                    AutoSize = true,
                    Margin = new Padding(10, 15, 0, 5)
                };
                pnlResultados.Controls.Add(lblTitulo);

                Guna2DataGridView grid = new Guna2DataGridView
                {
                    ReadOnly = true,
                    AllowUserToAddRows = false,
                    AllowUserToDeleteRows = false,
                    RowHeadersVisible = false,
                    Width = anchoTotal,
                    AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                    ScrollBars = ScrollBars.None,

                    ThemeStyle = {
                        HeaderStyle = { BackColor = Color.FromArgb(64, 64, 64), ForeColor = Color.White, Height = 35 },
                        RowsStyle = { Font = new Font("Segoe UI", 10), Height = 30 },
                        GridColor = Color.LightGray
                    }
                };

                grid.Columns.Add("colBase", "Base");
                foreach (string nombreCol in iteracion.NombresColumnas)
                {
                    grid.Columns.Add($"col{nombreCol}", nombreCol);
                }

                // Llenar Filas
                int filas = iteracion.Matriz.GetLength(0);
                int cols = iteracion.Matriz.GetLength(1);

                for (int i = 0; i < filas; i++)
                {
                    object[] filaDatos = new object[cols + 1];
                    filaDatos[0] = iteracion.NombresBasicas[i];

                    for (int j = 0; j < cols; j++)
                    {
                        double valor = iteracion.Matriz[i, j];
                        //formato condicional para enteros o decimales
                        filaDatos[j + 1] = (valor % 1 == 0) ? valor.ToString("0") : Math.Round(valor, 4).ToString();
                    }
                    grid.Rows.Add(filaDatos);
                }

                //pintar Pivote
                if (iteracion.FilaPivote != -1 && iteracion.ColumnaPivote != -1)
                {
                    int f = iteracion.FilaPivote;
                    int c = iteracion.ColumnaPivote + 1;
                    grid.Rows[f].Cells[c].Style.BackColor = Color.LightGreen;
                    grid.Rows[f].Cells[c].Style.ForeColor = Color.Black;
                    grid.Rows[f].Cells[c].Style.Font = new Font("Segoe UI", 10, FontStyle.Bold);
                }

                //calcular Altura
                int totalHeight = grid.ColumnHeadersHeight;
                foreach (DataGridViewRow row in grid.Rows) totalHeight += row.Height;
                grid.Height = totalHeight + 2;

                pnlResultados.Controls.Add(grid);
            }
            Guna2Separator sep = new Guna2Separator
            {
                Width = pnlResultados.Width - 20,
                FillThickness = 2,
                Margin = new Padding(10, 20, 10, 10)
            };
            pnlResultados.Controls.Add(sep);

            //titulo de Resultados
            Label lblRes = new Label
            {
                Text = "SOLUCIÓN OPTIMA:",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 150, 136),
                AutoSize = true,
                Margin = new Padding(10, 5, 0, 10)
            };
            pnlResultados.Controls.Add(lblRes);

            //panel para agrupar las variables
            FlowLayoutPanel pnlVariables = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                AutoSize = true,
                Width = pnlResultados.Width - 20,
                Margin = new Padding(10, 0, 0, 10)
            };
            for (int i = 0; i < resultado.VariablesDecision.Length; i++)
            {
                double valor = Math.Round(resultado.VariablesDecision[i], 4);
                Label lblVar = new Label
                {
                    Text = $"x{i + 1} = {valor}      ", // Espacios para separar
                    Font = new Font("Segoe UI", 11, FontStyle.Regular),
                    ForeColor = Color.Black,
                    AutoSize = true,
                    Margin = new Padding(0, 0, 15, 5)
                };
                pnlVariables.Controls.Add(lblVar);
            }
            pnlResultados.Controls.Add(pnlVariables);

            //valor Z
            Label lblTotal = new Label
            {
                Text = $"VALOR Z ÓPTIMO =  {Math.Round(resultado.ValorZ, 4)}",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Padding = new Padding(15, 10, 15, 10),
                Margin = new Padding(10, 10, 0, 20),
                BackColor = Color.FromArgb(64, 64, 64), // Fondo oscuro elegante
                TextAlign = ContentAlignment.MiddleCenter
            };
            pnlResultados.Controls.Add(lblTotal);
            tabPage1.ScrollControlIntoView(pnlResultados);
        }

        private void Form1_SizeChanged(object sender, EventArgs e)
        {
            tabPage1.SizeChanged += (s, e) =>
            {
                int nuevoAncho = tabPage1.ClientSize.Width - 40;
                pnlResultados.Width = nuevoAncho;
                foreach (Control c in pnlResultados.Controls)
                {
                    if (c is Guna2DataGridView grid)
                    {
                        grid.Width = nuevoAncho;
                    }
                }
            };
        }

        /**Metodo hundaro**/

        private void DimensionesCambiadas(object sender, EventArgs e)
        {
            int filas = (int)numFilas.Value;
            int cols = (int)numColumnas.Value;
            GenerarCuadricula(filas, cols);
        }
        private void GenerarCuadricula(int filas, int cols)
        {
            //diccionario temporal para guardar
            var datosPrevios = new Dictionary<string, string>();

            foreach (Control c in pnlMatrizHungaro.Controls)
            {
                if (c is Guna2TextBox txt && txt.Tag != null)
                {
                    //usamos tag como coordenada
                    datosPrevios[txt.Tag.ToString()] = txt.Text;
                }
            }
            pnlMatrizHungaro.Controls.Clear();
            matrizTextBoxes = new Guna2TextBox[filas, cols];

            int boxSize = 60;
            int margin = 5;
            pnlMatrizHungaro.Width = (boxSize + (margin * 2)) * cols + 40;
            for (int i = 0; i < filas; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    Guna2TextBox txt = new Guna2TextBox
                    {
                        Width = boxSize,
                        Height = 40,
                        Margin = new Padding(margin),
                        PlaceholderText = $"C{i + 1},{j + 1}",
                        TextAlign = HorizontalAlignment.Center,
                        BorderRadius = 5,
                        Tag = $"{i},{j}"
                    };
                    if (datosPrevios.ContainsKey($"{i},{j}"))
                    {
                        txt.Text = datosPrevios[$"{i},{j}"];
                    }

                    // Guardar en matriz lógica y visual
                    matrizTextBoxes[i, j] = txt;
                    pnlMatrizHungaro.Controls.Add(txt);
                }
            }
        }

        private void guna2Button1_Click(object sender, EventArgs e)
        {
            try
            {
                int filas = (int)numFilas.Value;
                int cols = (int)numColumnas.Value;
                double[,] matriz = new double[filas, cols];
                for (int i = 0; i < filas; i++)
                {
                    for (int j = 0; j < cols; j++)
                    {
                        string texto = matrizTextBoxes[i, j].Text;
                        double val = 0;
                        if (!string.IsNullOrWhiteSpace(texto))
                        {
                            if (!double.TryParse(texto, NumberStyles.Any, CultureInfo.InvariantCulture, out val))
                            {
                                MessageBox.Show("Número inválido."); return;
                            }
                        }
                        matriz[i, j] = val;
                    }
                }
                HungarianSolver solver = new HungarianSolver();
                var resultado = solver.Resolver(matriz);

                MostrarPasosHungaro(resultado);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }
        private void MostrarPasosHungaro(ResultadoHungaro resultado)
        {
            pnlResultados2.Controls.Clear();
            if (pnlResultados2.Parent != null)
                pnlResultados2.Width = pnlResultados2.Parent.ClientSize.Width - 40;
            Label lblMain = new Label
            {
                Text = "DESARROLLO",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.Black,
                AutoSize = true,
                Margin = new Padding(5, 15, 0, 5)
            };
            pnlResultados2.Controls.Add(lblMain);
            foreach (var paso in resultado.Historial)
            {
                Label lblTitulo = new Label
                {
                    Text = paso.Titulo,
                    Font = new Font("Segoe UI", 10, FontStyle.Bold),
                    ForeColor = Color.FromArgb(64, 64, 64),
                    AutoSize = true,
                    Margin = new Padding(5, 10, 0, 2)
                };
                pnlResultados2.Controls.Add(lblTitulo);
                Guna2DataGridView grid = new Guna2DataGridView
                {
                    ReadOnly = true,
                    AllowUserToAddRows = false,
                    AllowUserToDeleteRows = false,
                    RowHeadersVisible = false,
                    ScrollBars = ScrollBars.None,
                    AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                    Width = pnlResultados2.Width - 20,
                    Margin = new Padding(5, 0, 5, 10),

                    RowTemplate = { Height = 28 },
                    ColumnHeadersHeight = 30,

                    ThemeStyle = {
                        HeaderStyle = { BackColor = Color.FromArgb(50, 50, 50), ForeColor = Color.White, Height = 30 },
                        RowsStyle = { Font = new Font("Segoe UI", 10), Height = 28, ForeColor = Color.Black },
                        GridColor = Color.LightGray
                    }
                };

                int rows = paso.Matriz.GetLength(0);
                int cols = paso.Matriz.GetLength(1);

                grid.Columns.Add("colW", ""); 
                for (int k = 0; k < cols; k++) grid.Columns.Add($"col{k}", $"T{k + 1}");

                for (int i = 0; i < rows; i++)
                {
                    object[] rowData = new object[cols + 1];
                    rowData[0] = $"W{i + 1}";

                    for (int j = 0; j < cols; j++)
                    {
                        double val = paso.Matriz[i, j];

                        rowData[j + 1] = (val % 1 == 0) ? val.ToString("0") : val.ToString("0.##");
                    }
                    grid.Rows.Add(rowData);

                    // --- C. VISUALIZACIÓN (Ceros y Líneas) ---
                    for (int j = 0; j < cols; j++)
                    {
                        // 1. Pintar Fondo si hay líneas (Rosado suave = tachado)
                        // Filas tachadas
                        if (paso.FilasTachadas.Contains(i))
                            grid.Rows[i].Cells[j + 1].Style.BackColor = Color.FromArgb(245, 220, 220);

                        // Columnas tachadas
                        if (paso.ColsTachadas.Contains(j))
                        {
                            // Si ya estaba pintado (intersección), un poco más oscuro
                            if (grid.Rows[i].Cells[j + 1].Style.BackColor == Color.FromArgb(245, 220, 220))
                                grid.Rows[i].Cells[j + 1].Style.BackColor = Color.FromArgb(230, 190, 190);
                            else
                                grid.Rows[i].Cells[j + 1].Style.BackColor = Color.FromArgb(245, 220, 220);
                        }

                        // 2. Resaltar Ceros (Negrita)
                        // Solo resaltamos ceros si NO estamos tachando líneas (para limpieza visual)
                        if (Math.Abs(paso.Matriz[i, j]) < 0.0001)
                        {
                            grid.Rows[i].Cells[j + 1].Style.Font = new Font("Segoe UI", 9, FontStyle.Bold);
                            // Si no está tachado, píntalo amarillo suave para verlo fácil
                            if (grid.Rows[i].Cells[j + 1].Style.BackColor.A == 0)
                                grid.Rows[i].Cells[j + 1].Style.BackColor = Color.FromArgb(255, 255, 220);
                        }
                    }
                }

                // Calcular Altura exacta
                int h = grid.ColumnHeadersHeight + (rows * grid.RowTemplate.Height) + 2;
                grid.Height = h;

                pnlResultados2.Controls.Add(grid);
            }
            Guna2Separator sep = new Guna2Separator
            {
                Width = pnlResultados2.Width - 20,
                FillThickness = 2,
                Margin = new Padding(10, 20, 10, 10)
            };
            pnlResultados2.Controls.Add(sep);

            Label lblRes = new Label
            {
                Text = "RESULTADO ÓPTIMO:",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 150, 136), // Verde Húngaro
                AutoSize = true,
                Margin = new Padding(10, 5, 0, 10)
            };
            pnlResultados2.Controls.Add(lblRes);


            foreach (string asignacion in resultado.Asignaciones)
            {
                Label lblAsig = new Label
                {
                    Text = "• " + asignacion,
                    Font = new Font("Segoe UI", 10, FontStyle.Regular),
                    ForeColor = Color.Black,
                    AutoSize = true,
                    Margin = new Padding(20, 2, 0, 2)
                };
                pnlResultados2.Controls.Add(lblAsig);
            }


            Label lblTotal = new Label
            {
                Text = $"COSTO TOTAL MÍNIMO:  {resultado.CostoTotal}",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Color.Black,
                AutoSize = true,
                Padding = new Padding(10),
                Margin = new Padding(10, 15, 0, 20),
                BackColor = Color.FromArgb(240, 240, 240), // Fondo gris suave
                BorderStyle = BorderStyle.None // O FixedSingle si prefieres cuadro
            };
            pnlResultados2.Controls.Add(lblTotal);
            (pnlResultados2.Parent as ScrollableControl)?.ScrollControlIntoView(pnlResultados2);
        }
    }
}
