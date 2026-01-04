using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Threading;

namespace TapoControllerGUI
{
    public partial class ProfessionalTapoForm : Form
    {
        private DataGridView dgvCameras = null!;
        private Button btnScanNetwork = null!;
        private Button btnStopScan = null!;
        private Button btnRefresh = null!;
        private TextBox txtSubnet = null!;
        private CancellationTokenSource? scanCancellation;
        private GroupBox grpNetworkScan = null!;
        private RichTextBox rtbLog = null!;
        private StatusStrip statusStrip = null!;
        private ToolStripStatusLabel lblStatus = null!;
        private Panel pnlVideoStream = null!;
        private ComboBox cmbStreamType = null!;
        private Button btnStartStream = null!;
        private Button btnStopStream = null!;
        private Label lblStreamPlaceholder = null!;

        private List<TapoCamera> discoveredCameras = new List<TapoCamera>();

        public ProfessionalTapoForm()
        {
            InitializeComponent();
            SetupProfessionalTheme();
        }

        private void InitializeComponent()
        {
            this.Text = "Tapo Camera Management System";
            this.Size = new Size(1000, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MinimumSize = new Size(800, 600);
            this.BackColor = Color.FromArgb(240, 240, 240);

            CreateMainLayout();
            CreateStatusBar();
        }

        private void SetupProfessionalTheme()
        {
            this.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
            this.BackColor = SystemColors.Control;
        }

        private void CreateMainLayout()
        {
            var mainContainer = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Padding = new Padding(10),
                BackColor = SystemColors.Control
            };

            mainContainer.RowStyles.Add(new RowStyle(SizeType.Percent, 60F));
            mainContainer.RowStyles.Add(new RowStyle(SizeType.Percent, 40F));

            var topPanel = CreateTopPanel();
            var bottomPanel = CreateBottomPanel();

            mainContainer.Controls.Add(topPanel, 0, 0);
            mainContainer.Controls.Add(bottomPanel, 0, 1);

            this.Controls.Add(mainContainer);
        }

        private Control CreateTopPanel()
        {
            grpNetworkScan = new GroupBox
            {
                Text = "Network Camera Discovery",
                Dock = DockStyle.Fill,
                Padding = new Padding(10),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };

            var container = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 2,
                ColumnCount = 1
            };
            
            container.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            container.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var toolbar = new FlowLayoutPanel
            {
                Height = 40,
                Dock = DockStyle.Top,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Padding = new Padding(0, 5, 0, 5)
            };

            btnScanNetwork = new Button
            {
                Text = "Scan Network",
                Width = 120,
                Height = 30,
                FlatStyle = FlatStyle.Standard,
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F)
            };
            btnScanNetwork.Click += BtnScanNetwork_Click;

            btnStopScan = new Button
            {
                Text = "Stop Scan",
                Width = 80,
                Height = 30,
                FlatStyle = FlatStyle.Standard,
                BackColor = Color.FromArgb(220, 50, 50),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F),
                Enabled = false,
                Margin = new Padding(5, 0, 0, 0)
            };
            btnStopScan.Click += BtnStopScan_Click;

            btnRefresh = new Button
            {
                Text = "Refresh",
                Width = 80,
                Height = 30,
                FlatStyle = FlatStyle.Standard,
                BackColor = SystemColors.Control,
                Margin = new Padding(10, 0, 0, 0)
            };
            btnRefresh.Click += BtnRefresh_Click;

            var lblSubnet = new Label
            {
                Text = "Subnet:",
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(20, 7, 5, 0)
            };

            txtSubnet = new TextBox
            {
                Text = GetSubnet(GetLocalIPAddress()),
                Width = 100,
                Margin = new Padding(0, 5, 0, 0)
            };

            toolbar.Controls.Add(btnScanNetwork);
            toolbar.Controls.Add(btnStopScan);
            toolbar.Controls.Add(btnRefresh);
            toolbar.Controls.Add(lblSubnet);
            toolbar.Controls.Add(txtSubnet);

            dgvCameras = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                RowHeadersVisible = false,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.Fixed3D,
                GridColor = Color.LightGray,
                Font = new Font("Segoe UI", 8.5F)
            };

            dgvCameras.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "IPAddress",
                HeaderText = "IP Address",
                Width = 120
            });

            dgvCameras.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Model",
                HeaderText = "Model",
                Width = 150
            });

            dgvCameras.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Status",
                HeaderText = "Status",
                Width = 100
            });

            dgvCameras.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "LastSeen",
                HeaderText = "Last Seen",
                Width = 150
            });

            dgvCameras.SelectionChanged += DgvCameras_SelectionChanged;

            container.Controls.Add(toolbar, 0, 0);
            container.Controls.Add(dgvCameras, 0, 1);
            grpNetworkScan.Controls.Add(container);

            return grpNetworkScan;
        }

        private Control CreateBottomPanel()
        {
            var bottomContainer = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1
            };

            bottomContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));
            bottomContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F));

            var logPanel = CreateLogPanel();
            var streamPanel = CreateStreamPanel();

            bottomContainer.Controls.Add(logPanel, 0, 0);
            bottomContainer.Controls.Add(streamPanel, 1, 0);

            return bottomContainer;
        }

        private Control CreateLogPanel()
        {
            var grpLog = new GroupBox
            {
                Text = "Activity Log",
                Dock = DockStyle.Fill,
                Padding = new Padding(10),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };

            rtbLog = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BackColor = Color.White,
                ForeColor = Color.Black,
                Font = new Font("Consolas", 9F),
                BorderStyle = BorderStyle.Fixed3D
            };

            grpLog.Controls.Add(rtbLog);
            LogMessage("System initialized. Ready to scan for cameras.");
            
            return grpLog;
        }

        private Control CreateStreamPanel()
        {
            var grpCameraStream = new GroupBox
            {
                Text = "Live Camera Stream",
                Dock = DockStyle.Fill,
                Padding = new Padding(10),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };

            var container = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 3,
                ColumnCount = 1
            };

            container.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            container.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            container.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var streamControls = new FlowLayoutPanel
            {
                Height = 35,
                Dock = DockStyle.Top,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Padding = new Padding(0, 5, 0, 5)
            };

            streamControls.Controls.Add(new Label 
            { 
                Text = "Stream:", 
                AutoSize = true, 
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(0, 7, 5, 0)
            });
            
            cmbStreamType = new ComboBox
            {
                Width = 120,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Margin = new Padding(0, 5, 0, 0)
            };
            cmbStreamType.Items.AddRange(new[] { "Auto-detect", "RTSP", "HTTP" });
            cmbStreamType.SelectedIndex = 0;
            streamControls.Controls.Add(cmbStreamType);

            pnlVideoStream = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Black,
                BorderStyle = BorderStyle.Fixed3D
            };

            lblStreamPlaceholder = new Label
            {
                Text = "Live Video Stream\n\nSelect a camera and click 'Start Stream'\nto view live video feed here",
                ForeColor = Color.White,
                BackColor = Color.Black,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10F)
            };
            
            pnlVideoStream.Controls.Add(lblStreamPlaceholder);

            var streamButtons = new FlowLayoutPanel
            {
                Height = 40,
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Padding = new Padding(0, 5, 0, 0)
            };

            btnStartStream = new Button
            {
                Text = "Start Stream",
                Width = 120,
                Height = 30,
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Standard,
                Enabled = false
            };
            btnStartStream.Click += BtnStartStream_Click;

            btnStopStream = new Button
            {
                Text = "Stop Stream",
                Width = 120,
                Height = 30,
                BackColor = Color.FromArgb(220, 50, 50),
                ForeColor = Color.White,
                Margin = new Padding(10, 0, 0, 0),
                Enabled = false
            };
            btnStopStream.Click += BtnStopStream_Click;

            streamButtons.Controls.Add(btnStartStream);
            streamButtons.Controls.Add(btnStopStream);

            container.Controls.Add(streamControls, 0, 0);
            container.Controls.Add(pnlVideoStream, 0, 1);
            container.Controls.Add(streamButtons, 0, 2);

            grpCameraStream.Controls.Add(container);
            return grpCameraStream;
        }

        private void CreateStatusBar()
        {
            statusStrip = new StatusStrip
            {
                BackColor = SystemColors.Control
            };

            lblStatus = new ToolStripStatusLabel
            {
                Text = "Ready",
                Spring = true,
                TextAlign = ContentAlignment.MiddleLeft
            };

            statusStrip.Items.Add(lblStatus);
            this.Controls.Add(statusStrip);
        }

        // Event Handlers
        private async void BtnScanNetwork_Click(object? sender, EventArgs e)
        {
            btnScanNetwork.Enabled = false;
            btnStopScan.Enabled = true;
            btnScanNetwork.Text = "Scanning...";
            lblStatus.Text = "Scanning network for Tapo cameras...";
            
            try
            {
                LogMessage("Starting network scan for Tapo cameras...");
                discoveredCameras.Clear();
                dgvCameras.DataSource = null;
                
                scanCancellation = new CancellationTokenSource();
                await ScanForTapoCameras(scanCancellation.Token);
                
                if (!scanCancellation.Token.IsCancellationRequested)
                {
                    dgvCameras.DataSource = discoveredCameras;
                    lblStatus.Text = $"Scan complete - Found {discoveredCameras.Count} camera(s)";
                    LogMessage($"Network scan completed. Found {discoveredCameras.Count} Tapo camera(s).");
                }
                else
                {
                    lblStatus.Text = "Scan cancelled";
                    LogMessage("Network scan cancelled by user.");
                }
            }
            catch (OperationCanceledException)
            {
                lblStatus.Text = "Scan cancelled";
                LogMessage("Network scan cancelled by user.");
            }
            catch (Exception ex)
            {
                LogMessage($"Error during network scan: {ex.Message}");
                lblStatus.Text = "Scan failed";
            }
            finally
            {
                btnScanNetwork.Enabled = true;
                btnStopScan.Enabled = false;
                btnScanNetwork.Text = "Scan Network";
                scanCancellation?.Dispose();
                scanCancellation = null;
            }
        }

        private void BtnStopScan_Click(object? sender, EventArgs e)
        {
            scanCancellation?.Cancel();
            btnStopScan.Enabled = false;
            LogMessage("Stopping network scan...");
        }

        private void BtnRefresh_Click(object? sender, EventArgs e)
        {
            dgvCameras.Refresh();
            LogMessage("Camera list refreshed.");
        }

        private void DgvCameras_SelectionChanged(object? sender, EventArgs e)
        {
            bool hasSelection = dgvCameras.SelectedRows.Count > 0;
            btnStartStream.Enabled = hasSelection;
            
            if (hasSelection && dgvCameras.SelectedRows[0].DataBoundItem is TapoCamera camera)
            {
                LogMessage($"Selected camera: {camera.IPAddress} ({camera.Model})");
            }
        }

        private void BtnStartStream_Click(object? sender, EventArgs e)
        {
            if (dgvCameras.SelectedRows.Count > 0)
            {
                var camera = dgvCameras.SelectedRows[0].DataBoundItem as TapoCamera;
                if (camera != null)
                {
                    lblStreamPlaceholder.Text = $"Connecting to camera stream...\n{camera.IPAddress}";
                    LogMessage($"Starting stream from {camera.IPAddress}");
                    btnStartStream.Enabled = false;
                    btnStopStream.Enabled = true;
                }
            }
        }

        private void BtnStopStream_Click(object? sender, EventArgs e)
        {
            lblStreamPlaceholder.Text = "Live Video Stream\n\nSelect a camera and click 'Start Stream'\nto view live video feed here";
            LogMessage("Stream stopped.");
            btnStartStream.Enabled = true;
            btnStopStream.Enabled = false;
        }

        // Core Functionality
        private async Task ScanForTapoCameras(CancellationToken cancellationToken = default)
        {
            var subnet = txtSubnet.Text.Trim();
            
            if (string.IsNullOrWhiteSpace(subnet) || subnet.Split('.').Length != 3)
            {
                LogMessage("Invalid subnet format. Using auto-detected subnet.");
                subnet = GetSubnet(GetLocalIPAddress());
                txtSubnet.Text = subnet;
            }
            
            LogMessage($"Scanning subnet: {subnet}.0/24");
            
            var tasks = new List<Task>();
            var semaphore = new SemaphoreSlim(100);
            
            for (int i = 1; i <= 254; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;
                    
                var ip = $"{subnet}.{i}";
                
                tasks.Add(Task.Run(async () =>
                {
                    await semaphore.WaitAsync(cancellationToken);
                    try
                    {
                        if (!cancellationToken.IsCancellationRequested)
                        {
                            await ScanSingleIP(ip, cancellationToken);
                        }
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }, cancellationToken));
            }
            
            await Task.WhenAll(tasks);
        }

        private async Task ScanSingleIP(string ip, CancellationToken cancellationToken = default)
        {
            try
            {
                if (cancellationToken.IsCancellationRequested) return;
                
                using var ping = new Ping();
                var reply = await ping.SendPingAsync(ip, 200);
                bool isReachable = reply.Status == IPStatus.Success;
                
                if (isReachable && !cancellationToken.IsCancellationRequested)
                {
                    if (await IsTapoCamera(ip, cancellationToken))
                    {
                        var camera = new TapoCamera
                        {
                            IPAddress = ip,
                            Model = await DetectCameraModel(ip, cancellationToken),
                            Status = "Online",
                            LastSeen = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                        };
                        
                        discoveredCameras.Add(camera);
                        
                        if (!cancellationToken.IsCancellationRequested)
                        {
                            this.Invoke(new Action(() =>
                            {
                                LogMessage($"Found Tapo camera at {ip} - Model: {camera.Model}");
                                dgvCameras.DataSource = null;
                                dgvCameras.DataSource = discoveredCameras;
                            }));
                        }
                    }
                }
            }
            catch when (cancellationToken.IsCancellationRequested)
            {
            }
            catch
            {
            }
        }

        private async Task<bool> IsTapoCamera(string ip, CancellationToken cancellationToken = default)
        {
            try
            {
                using var client = new System.Net.Http.HttpClient();
                client.Timeout = TimeSpan.FromMilliseconds(800);
                
                var portsToCheck = new List<int> { 443, 80 };
                
                foreach (var port in portsToCheck)
                {
                    var baseUrl = port == 443 ? $"https://{ip}" : $"http://{ip}";
                    
                    var tapoEndpoints = new[]
                    {
                        $"{baseUrl}/stok=/ds",
                        $"{baseUrl}/cgi-bin/luci/web",
                        baseUrl
                    };
                    
                    foreach (var endpoint in tapoEndpoints)
                    {
                        try
                        {
                            var response = await client.GetAsync(endpoint);
                            var content = await response.Content.ReadAsStringAsync();
                            
                            if (content.Contains("tapo", StringComparison.OrdinalIgnoreCase) ||
                                content.Contains("tp-link", StringComparison.OrdinalIgnoreCase) ||
                                content.Contains("SMART.IPCAMERA", StringComparison.OrdinalIgnoreCase))
                            {
                                return true;
                            }
                        }
                        catch
                        {
                            continue;
                        }
                    }
                }
            }
            catch
            {
            }
            
            return false;
        }

        private async Task<string> DetectCameraModel(string ip, CancellationToken cancellationToken = default)
        {
            await Task.Delay(10, cancellationToken);
            return "Tapo Camera";
        }

        private void LogMessage(string message)
        {
            if (rtbLog.InvokeRequired)
            {
                rtbLog.Invoke(new Action(() => LogMessage(message)));
                return;
            }

            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            rtbLog.AppendText($"[{timestamp}] {message}\n");
            rtbLog.ScrollToCaret();
        }

        private string GetLocalIPAddress()
        {
            try
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        return ip.ToString();
                    }
                }
            }
            catch { }
            return "192.168.1.1";
        }

        private string GetSubnet(string ipAddress)
        {
            var parts = ipAddress.Split('.');
            if (parts.Length >= 3)
            {
                return $"{parts[0]}.{parts[1]}.{parts[2]}";
            }
            return "192.168.1";
        }
    }

    public class TapoCamera
    {
        public string IPAddress { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string LastSeen { get; set; } = string.Empty;
    }
}
