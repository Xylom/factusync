using System;
using System.Collections.Generic;

namespace FactuSync.Shared
{
    public class Cliente
    {
        // Propiedades para la UI
        public string Codigo => CODCLI?.ToString() ?? string.Empty;
        public string Nombre => !string.IsNullOrEmpty(NOFCLI) ? NOFCLI : NOCCLI; // Muestra el Nombre Fiscal prioritariamente
        public string CifDni => NIFCLI;
        public string Telefono => TELCLI;
        public string Email => EMACLI;
        public string Direccion => DOMCLI;
        public double REQCLI { get; set; } // 1 = Tiene Recargo, 0 = No
        public double TARCLI { get; set; } // Código de Tarifa assigned to client

        // === MAPEO EXACTO DE TABLA F_CLI ===
        public double? CODCLI { get; set; }
        public double? CCOCLI { get; set; }
        public string NIFCLI { get; set; } = string.Empty;
        public string NOFCLI { get; set; } = string.Empty;
        public string NOCCLI { get; set; } = string.Empty;
        public string DOMCLI { get; set; } = string.Empty;
        public string POBCLI { get; set; } = string.Empty;
        public string CPOCLI { get; set; } = string.Empty;
        public string PROCLI { get; set; } = string.Empty;
        public string TELCLI { get; set; } = string.Empty;
        public string FAXCLI { get; set; } = string.Empty;
        public string PCOCLI { get; set; } = string.Empty;
        public double? AGECLI { get; set; }
        public string BANCLI { get; set; } = string.Empty;
        public string ENTCLI { get; set; } = string.Empty;
        public string OFICLI { get; set; } = string.Empty;
        public string DCOCLI { get; set; } = string.Empty;
        public string CUECLI { get; set; } = string.Empty;
        public string FPACLI { get; set; } = string.Empty;
        public double? DP1CLI { get; set; }
        public double? DP2CLI { get; set; }
        public double? DP3CLI { get; set; }
        public string TCLCLI { get; set; } = string.Empty;
        public decimal? DT1CLI { get; set; }
        public decimal? DT2CLI { get; set; }
        public decimal? DT3CLI { get; set; }
        public double? TESCLI { get; set; }
        public string CPRCLI { get; set; } = string.Empty;
        public double? TPOCLI { get; set; }
        public string PORCLI { get; set; } = string.Empty;
        public double? IVACLI { get; set; }
        public double? TIVCLI { get; set; }
        public DateTime? FALCLI { get; set; }
        public string EMACLI { get; set; } = string.Empty;
        public string WEBCLI { get; set; } = string.Empty;
        public string MEMCLI { get; set; } = string.Empty;
        public string OBSCLI { get; set; } = string.Empty;
        public string HORCLI { get; set; } = string.Empty;
        public string VDECLI { get; set; } = string.Empty;
        public string VHACLI { get; set; } = string.Empty;
        public double? CRFCLI { get; set; }
        public double? NVCCLI { get; set; }
        public double? NFCCLI { get; set; }
        public double? NICCLI { get; set; }
        public double? MONCLI { get; set; }
        public string PAICLI { get; set; } = string.Empty;
        public double? DOCCLI { get; set; }
        public string DBACLI { get; set; } = string.Empty;
        public string PBACLI { get; set; } = string.Empty;
        public string SWFCLI { get; set; } = string.Empty;
        public string CO1CLI { get; set; } = string.Empty;
        public string CO2CLI { get; set; } = string.Empty;
        public string CO3CLI { get; set; } = string.Empty;
        public string CO4CLI { get; set; } = string.Empty;
        public string CO5CLI { get; set; } = string.Empty;
        public decimal? IM1CLI { get; set; }
        public decimal? IM2CLI { get; set; }
        public decimal? IM3CLI { get; set; }
        public decimal? IM4CLI { get; set; }
        public decimal? IM5CLI { get; set; }
        public string RUTCLI { get; set; } = string.Empty;
        public string? NombreRuta { get; set; }
        public string SWICLI { get; set; } = string.Empty;
        public string GIRCLI { get; set; } = string.Empty;
        public string CUWCLI { get; set; } = string.Empty;
        public string CAWCLI { get; set; } = string.Empty;
        public double? SUWCLI { get; set; }
        public string MEWCLI { get; set; } = string.Empty;
        public double? ESTCLI { get; set; }
        public string AR1CLI { get; set; } = string.Empty;
        public string AR2CLI { get; set; } = string.Empty;
        public string AR3CLI { get; set; } = string.Empty;
        public string AR4CLI { get; set; } = string.Empty;
        public string AR5CLI { get; set; } = string.Empty;
        public double? FELCLI { get; set; }
        public double? TRACLI { get; set; }
        public double? NCFCLI { get; set; }
        public DateTime? FNACLI { get; set; }
        public string FOTCLI { get; set; } = string.Empty;
        public string SKYCLI { get; set; } = string.Empty;
        public string NO1CLI { get; set; } = string.Empty;
        public string TF1CLI { get; set; } = string.Empty;
        public string EM1CLI { get; set; } = string.Empty;
        public string NO2CLI { get; set; } = string.Empty;
        public string TF2CLI { get; set; } = string.Empty;
        public string EM2CLI { get; set; } = string.Empty;
        public string NO3CLI { get; set; } = string.Empty;
        public string TF3CLI { get; set; } = string.Empty;
        public string EM3CLI { get; set; } = string.Empty;
        public string NO4CLI { get; set; } = string.Empty;
        public string TF4CLI { get; set; } = string.Empty;
        public string EM4CLI { get; set; } = string.Empty;
        public string NO5CLI { get; set; } = string.Empty;
        public string TF5CLI { get; set; } = string.Empty;
        public string EM5CLI { get; set; } = string.Empty;
        public double? RETCLI { get; set; }
        public double? CTMCLI { get; set; }
        public double? MNPCLI { get; set; }
        public double? IFICLI { get; set; }
        public double? IMPCLI { get; set; }
        public double? NCACLI { get; set; }
        public decimal? CAMCLI { get; set; }
        public string CO6CLI { get; set; } = string.Empty;
        public decimal? IM6CLI { get; set; }
        public string AR6CLI { get; set; } = string.Empty;
        public string CO7CLI { get; set; } = string.Empty;
        public decimal? IM7CLI { get; set; }
        public string AR7CLI { get; set; } = string.Empty;
        public string CO8CLI { get; set; } = string.Empty;
        public decimal? IM8CLI { get; set; }
        public string AR8CLI { get; set; } = string.Empty;
        public string CO9CLI { get; set; } = string.Empty;
        public decimal? IM9CLI { get; set; }
        public string AR9CLI { get; set; } = string.Empty;
        public string CO10CLI { get; set; } = string.Empty;
        public decimal? IM10CLI { get; set; }
        public string AR10CLI { get; set; } = string.Empty;
    }

    public class Ruta
    {
        public string CODRUT { get; set; } = string.Empty;
        public string DESRUT { get; set; } = string.Empty;
        public double? AGERUT { get; set; }
    }

    public class Proveedor
    {
        // Propiedades de la UI original para mantener compatibilidad
        public string Codigo => CODPRO?.ToString() ?? string.Empty;
        public string Nombre => !string.IsNullOrEmpty(NOCPRO) ? NOCPRO : NOFPRO;
        public string CifDni => NIFPRO;
        public string Telefono => TELPRO;

        // === MAPEO EXACTO DE TABLA F_PRO ===
        // Imagen 1
        public double? CODPRO { get; set; }
        public double? CCOPRO { get; set; }
        public double? TIPPRO { get; set; }
        public string NIFPRO { get; set; } = string.Empty;
        public string NOFPRO { get; set; } = string.Empty;
        public string NOCPRO { get; set; } = string.Empty;
        public string DOMPRO { get; set; } = string.Empty;
        public string POBPRO { get; set; } = string.Empty;
        public string CPOPRO { get; set; } = string.Empty;
        public string PROPRO { get; set; } = string.Empty;
        public string TELPRO { get; set; } = string.Empty;
        public string FAXPRO { get; set; } = string.Empty;
        public string PCOPRO { get; set; } = string.Empty;
        public string BANPRO { get; set; } = string.Empty;
        public string ENTPRO { get; set; } = string.Empty;
        public string OFIPRO { get; set; } = string.Empty;
        public string DCOPRO { get; set; } = string.Empty;
        public string CUEPRO { get; set; } = string.Empty;
        public string FPAPRO { get; set; } = string.Empty;
        public double? SAPPRO { get; set; }
        public double? DAPPRO { get; set; }
        public string TARPRO { get; set; } = string.Empty;
        public decimal? DT1PRO { get; set; }
        public decimal? DT2PRO { get; set; }
        public decimal? DT3PRO { get; set; }
        public string CCLPRO { get; set; } = string.Empty;

        // Imagen 2
        public double? TPOPRO { get; set; }
        public string PORPRO { get; set; } = string.Empty;
        public double? IVAPRO { get; set; }
        public string RESPRO { get; set; } = string.Empty;
        public decimal? RFIPRO { get; set; }
        public string PRAPRO { get; set; } = string.Empty;
        public DateTime? FALPRO { get; set; }
        public string WEBPRO { get; set; } = string.Empty;
        public string EMAPRO { get; set; } = string.Empty;
        public string OBSPRO { get; set; } = string.Empty;
        public string HORPRO { get; set; } = string.Empty;
        public string VDEPRO { get; set; } = string.Empty;
        public string VHAPRO { get; set; } = string.Empty;
        public double? NVAPRO { get; set; }
        public double? NRPPRO { get; set; }
        public double? NIPPRO { get; set; }
        public string PAIPRO { get; set; } = string.Empty;
        public string SWFPRO { get; set; } = string.Empty;
        public string TLXPRO { get; set; } = string.Empty;
        public string DBAPRO { get; set; } = string.Empty;
        public string PBAPRO { get; set; } = string.Empty;
        public double? REQPRO { get; set; }
        public string CCEPRO { get; set; } = string.Empty;
        public double? DP1PRO { get; set; }

        // Imagen 3
        public double? DP2PRO { get; set; }
        public double? DP3PRO { get; set; }
        public string SWIPRO { get; set; } = string.Empty;
        public string MEMPRO { get; set; } = string.Empty;
        public decimal? RETPRO { get; set; }
        public string NO1PRO { get; set; } = string.Empty;
        public string TF1PRO { get; set; } = string.Empty;
        public string EM1PRO { get; set; } = string.Empty;
        public string NO2PRO { get; set; } = string.Empty;
        public string TF2PRO { get; set; } = string.Empty;
        public string EM2PRO { get; set; } = string.Empty;
        public string NO3PRO { get; set; } = string.Empty;
        public string TF3PRO { get; set; } = string.Empty;
        public string EM3PRO { get; set; } = string.Empty;
        public string NO4PRO { get; set; } = string.Empty;
        public string TF4PRO { get; set; } = string.Empty;
        public string EM4PRO { get; set; } = string.Empty;
        public string NO5PRO { get; set; } = string.Empty;
        public string TF5PRO { get; set; } = string.Empty;
        public string EM5PRO { get; set; } = string.Empty;
        public double? DOCPRO { get; set; }
        public double? IFIPRO { get; set; }
        public double? IMPPRO { get; set; }
        public double? HOMPRO { get; set; }
    }

    public class Familia
    {
        public string CODFAM { get; set; } = string.Empty;
        public string DESFAM { get; set; } = string.Empty;
    }

    public class Articulo
    {
        // Propiedades de la UI original para mantener compatibilidad
        public string Codigo => CODART;
        public string Descripcion => DESART;
        public string Familia => FAMART;
        public decimal PrecioVenta { get; set; } // Retenido para mapeo con F_TAR
        public bool PrecioConIva { get; set; }
        public string DisplayText => $"{CODART} - {(Descripcion ?? "Sin nombre")} | ${PrecioVenta:N2}";
        public decimal? PRELTA_TAR_RAW { get; set; }
        public decimal Stock => Convert.ToDecimal(STOART ?? 0); 
        public int IvaIndex { get; set; }

        // === MAPEO EXACTO DE TABLA F_ART ===
        // Imagen 1
        public string CODART { get; set; } = string.Empty;
        public string EQUART { get; set; } = string.Empty;
        public double? CCOART { get; set; }
        public string FAMART { get; set; } = string.Empty;
        public string DESART { get; set; } = string.Empty;
        public string DEEART { get; set; } = string.Empty;
        public string DETART { get; set; } = string.Empty;
        public double? PHAART { get; set; }
        public double? TIVART { get; set; }
        public decimal? PCOART { get; set; }
        public decimal? DT0ART { get; set; }
        public decimal? DT1ART { get; set; }
        public decimal? DT2ART { get; set; }
        public DateTime? FALART { get; set; }
        public decimal? MDAART { get; set; }
        public string UBIART { get; set; } = string.Empty;
        public double? UELART { get; set; }
        public decimal? UPPART { get; set; }
        public string DIMART { get; set; } = string.Empty;
        public string MEMART { get; set; } = string.Empty;
        public string OBSART { get; set; } = string.Empty;
        public double? NPUART { get; set; }
        public double? NIAART { get; set; }
        public double? COMART { get; set; }
        public string CP1ART { get; set; } = string.Empty;
        public string CP2ART { get; set; } = string.Empty;
        public string CP3ART { get; set; } = string.Empty;
        public string DLAART { get; set; } = string.Empty;
        public decimal? IPUART { get; set; }
        public string NCCART { get; set; } = string.Empty;

        // Imagen 2
        public string CUCART { get; set; } = string.Empty;
        public decimal? CANART { get; set; }
        public string IMGART { get; set; } = string.Empty;
        public double? SUWART { get; set; }
        public string DEWART { get; set; } = string.Empty;
        public string MEWART { get; set; } = string.Empty;
        public double? CSTART { get; set; }
        public string IMWART { get; set; } = string.Empty;
        public double? STOART { get; set; }
        public DateTime? FUMART { get; set; }
        public decimal? PESART { get; set; }
        public double? FTEART { get; set; }
        public string ACOART { get; set; } = string.Empty;
        public string GARART { get; set; } = string.Empty;
        public double? UMEART { get; set; }
        public double? TMOART { get; set; }
        public string CONART { get; set; } = string.Empty;
        public double? TIV2ART { get; set; }
        public string DE1ART { get; set; } = string.Empty;
        public string DE2ART { get; set; } = string.Empty;
        public string DE3ART { get; set; } = string.Empty;
        public decimal? DFIART { get; set; }
        public double? RPUART { get; set; }
        public decimal? RPFART { get; set; }
        public double? RCUART { get; set; }
        public decimal? RCFART { get; set; }
        public string REFART { get; set; } = string.Empty;

        // Imagen 3
        public string MECART { get; set; } = string.Empty;
        public double? DSCART { get; set; }
        public string EANART { get; set; } = string.Empty;
        public double? AMAART { get; set; }
        public decimal? CAEART { get; set; }
        public double? UFSART { get; set; }
        public double? IMFART { get; set; }
        public string DELART { get; set; } = string.Empty;
        public decimal? PFIART { get; set; }
        public double? MPTART { get; set; }
        public string CP4ART { get; set; } = string.Empty;
        public string CP5ART { get; set; } = string.Empty;
        public double? ORDART { get; set; }
        public string UEQART { get; set; } = string.Empty;
        public double? DCOART { get; set; }
        public string FAVART { get; set; } = string.Empty;
        public double? DSTART { get; set; }
        public decimal? VEWART { get; set; }
        public string URAART { get; set; } = string.Empty;
        public decimal? VMPART { get; set; }
        public string UR1ART { get; set; } = string.Empty;
        public string UR2ART { get; set; } = string.Empty;
        public string UR3ART { get; set; } = string.Empty;
        public string CN8ART { get; set; } = string.Empty;
        public double? OCUART { get; set; }
        public double? RSVART { get; set; }
    }

    public class Almacen
    {
        public string Codigo { get; set; } = "";
        public string Nombre { get; set; } = "";
    }

    public class Pedido
    {
        // Propiedades de la UI original para mantener compatibilidad
        public int Id { get; set; } // Opcional, Factusol usa CODPCL/TIPPCL
        public string CodigoCliente 
        { 
            get => CLIPCL?.ToString() ?? string.Empty; 
            set => CLIPCL = double.TryParse(value, out var v) ? v : null; 
        }
        public string NombreCliente 
        { 
            get => CNOPCL; 
            set => CNOPCL = value; 
        }
        public DateTime Fecha 
        { 
            get => FECPCL ?? DateTime.Now; 
            set => FECPCL = value; 
        }
        public decimal Total 
        { 
            get => TOTPCL ?? 0; 
            set => TOTPCL = value; 
        }
        public List<PedidoLinea> Lineas { get; set; } = new List<PedidoLinea>();

        // === MAPEO EXACTO DE TABLA F_PCL ===
        // Imagen 1
        public string TIPPCL { get; set; } = string.Empty;
        public double? CODPCL { get; set; }
        public string ALMPCL { get; set; } = string.Empty;
        public double? ESTPCL { get; set; }
        public string REFPCL { get; set; } = string.Empty;
        public DateTime? FECPCL { get; set; }
        public double? AGEPCL { get; set; }
        public string PROPCL { get; set; } = string.Empty;
        public double? CLIPCL { get; set; }
        public double REQCLI { get; set; } // Recargo
        public double TARCLI { get; set; } // Tarifa
        public string CNOPCL { get; set; } = string.Empty;
        public string CDOPCL { get; set; } = string.Empty;
        public string CPOPCL { get; set; } = string.Empty;
        public string CCPPCL { get; set; } = string.Empty;
        public string CPRPCL { get; set; } = string.Empty;
        public string CNIPCL { get; set; } = string.Empty;
        public double? TIVPCL { get; set; }
        public double? REQPCL { get; set; }
        public string TELPCL { get; set; } = string.Empty;
        public decimal? NET1PCL { get; set; }
        public decimal? NET2PCL { get; set; }
        public decimal? NET3PCL { get; set; }
        public decimal? PDTO1PCL { get; set; }
        public decimal? PDTO2PCL { get; set; }
        public decimal? PDTO3PCL { get; set; }
        public decimal? IDTO1PCL { get; set; }
        public decimal? IDTO2PCL { get; set; }

        // Imagen 2
        public decimal? IDTO3PCL { get; set; }
        public decimal? PPPA1PCL { get; set; }
        public decimal? PPPA2PCL { get; set; }
        public decimal? PPPA3PCL { get; set; }
        public decimal? IPPA1PCL { get; set; }
        public decimal? IPPA2PCL { get; set; }
        public decimal? IPPA3PCL { get; set; }
        public decimal? PPOR1PCL { get; set; }
        public decimal? PPOR2PCL { get; set; }
        public decimal? PPOR3PCL { get; set; }
        public decimal? IPOR1PCL { get; set; }
        public decimal? IPOR2PCL { get; set; }
        public decimal? IPOR3PCL { get; set; }
        public decimal? PFIN1PCL { get; set; }
        public decimal? PFIN2PCL { get; set; }
        public decimal? PFIN3PCL { get; set; }
        public decimal? IFIN1PCL { get; set; }
        public decimal? IFIN2PCL { get; set; }
        public decimal? IFIN3PCL { get; set; }
        public decimal? BAS1PCL { get; set; }
        public decimal? BAS2PCL { get; set; }
        public decimal? BAS3PCL { get; set; }
        public decimal? PIVA1PCL { get; set; }
        public decimal? PIVA2PCL { get; set; }

        // Imagen 3
        public decimal? PIVA3PCL { get; set; }
        public decimal? IIVA1PCL { get; set; }
        public decimal? IIVA2PCL { get; set; }
        public decimal? IIVA3PCL { get; set; }
        public decimal? PREC1PCL { get; set; }
        public decimal? PREC2PCL { get; set; }
        public decimal? PREC3PCL { get; set; }
        public decimal? IREC1PCL { get; set; }
        public decimal? IREC2PCL { get; set; }
        public decimal? IREC3PCL { get; set; }
        public decimal? PRET1PCL { get; set; } // Porcentaje de retención
        public decimal? IRET1PCL { get; set; } // Importe de retención
        public decimal? TOTPCL { get; set; }
        public string FOPPCL { get; set; } = string.Empty; // Confirmado por captura de pantalla del usuario
        public string PENPCL { get; set; } = string.Empty;
        public double? PRTPCL { get; set; }
        public string TPOPCL { get; set; } = string.Empty;
        public string OB1PCL { get; set; } = string.Empty;
        public string OB2PCL { get; set; } = string.Empty;
        public double? OBRPCL { get; set; }
        public string PPOPCL { get; set; } = string.Empty;
        public string PRIPCL { get; set; } = string.Empty;
        public string ASOPCL { get; set; } = string.Empty;
        public string COMPCL { get; set; } = string.Empty;

        // Imagen 4
        public double? USUPCL { get; set; }
        public double? USMPCL { get; set; }
        public string FAXPCL { get; set; } = string.Empty;
        public decimal? NET4PCL { get; set; }
        public decimal? PDTO4PCL { get; set; }
        public decimal? IDTO4PCL { get; set; }
        public decimal? PPPA4PCL { get; set; }
        public decimal? IPPA4PCL { get; set; }
        public decimal? PPOR4PCL { get; set; }
        public decimal? IPOR4PCL { get; set; }
        public decimal? PFIN4PCL { get; set; }
        public decimal? IFIN4PCL { get; set; }
        public decimal? BAS4PCL { get; set; }
        public double? EMAPCL { get; set; }
        public string PASPCL { get; set; } = string.Empty;
        public DateTime? HORPCL { get; set; }
        public string CEMPCL { get; set; } = string.Empty;
        public string CPAPCL { get; set; } = string.Empty;
        public double? INCPCL { get; set; }
        public double? TIVA1PCL { get; set; }
        public double? TIVA2PCL { get; set; }
        public double? TIVA3PCL { get; set; }
        public double? TRNPCL { get; set; }
        public string TPVIDPCL { get; set; } = string.Empty;
    }

    public class PedidoLinea
    {
        // Propiedades de la UI original para mantener compatibilidad
        public string CodigoArticulo 
        { 
            get => ARTLPC; 
            set => ARTLPC = value; 
        }
        public string DescripcionArticulo 
        { 
            get => DESLPC; 
            set => DESLPC = value; 
        }
        public decimal Cantidad 
        { 
            get => CANLPC ?? 0; 
            set => CANLPC = value; 
        }
        public decimal Precio 
        { 
            get => PRELPC ?? 0; 
            set => PRELPC = value; 
        }
        public decimal Total => Cantidad * Precio; // Campo calculado en la UI

        // === MAPEO EXACTO DE TABLA F_LPC ===
        // Imagen 1
        public string TIPLPC { get; set; } = string.Empty;
        public double? CODLPC { get; set; }
        public double? POSLPC { get; set; }
        public int IvaIndex { get; set; }
        public string ARTLPC { get; set; } = string.Empty;
        public string DESLPC { get; set; } = string.Empty;
        public decimal? CANLPC { get; set; }
        public double? DT1LPC { get; set; }
        public double? DT2LPC { get; set; }
        public double? DT3LPC { get; set; }
        public decimal? PRELPC { get; set; }
        public decimal? TOTLPC { get; set; }
        public decimal? PENLPC { get; set; }
        public double? IVALPC { get; set; }
        public string DOCLPC { get; set; } = string.Empty;
        public string DTPLPC { get; set; } = string.Empty;
        public double? DCOLPC { get; set; }
        public string MEMLPC { get; set; } = string.Empty;
        public string EJELPC { get; set; } = string.Empty;
        public decimal? ALTLPC { get; set; }
        public decimal? ANCLPC { get; set; }
        public decimal? FONLPC { get; set; }
        public DateTime? FFALPC { get; set; }
        public DateTime? FCOLPC { get; set; }
        public double? IINLPC { get; set; }
        public decimal? PIVLPC { get; set; }
        public decimal? TIVLPC { get; set; }

        // Imagen 2
        public double? FIMLPC { get; set; }
        public decimal? COSLPC { get; set; }
        public decimal? BULLPC { get; set; }
        public string CE1LPC { get; set; } = string.Empty;
        public string CE2LPC { get; set; } = string.Empty;
        public string IMALPC { get; set; } = string.Empty;
        public string SUMLPC { get; set; } = string.Empty;
        public decimal? ANULPC { get; set; }
        public double? NIMLPC { get; set; }
    }

    public class Tarifa
    {
        // === MAPEO EXACTO DE TABLA F_TAR ===
        public double? CODTAR { get; set; }
        public string DESTAR { get; set; } = string.Empty;
        public decimal? MARTAR { get; set; }
        public double? IVATAR { get; set; }
    }

    public class TarifaLinea
    {
        // === MAPEO EXACTO DE TABLA F_LTA ===
        public double? TARLTA { get; set; }
        public string ARTLTA { get; set; } = string.Empty;
        public decimal? MARLTA { get; set; }
        public decimal? PRELTA { get; set; }
    }

    public class Agente
    {
        // Propiedades auxiliares para Auth
        public string CodigoUsuarioWeb => CUWAGE;
        public string ClaveUsuarioWeb => CAWAGE;
        public bool TieneAccesoWeb => SUWAGE == 1; // 1 = Sí, 0 = No

        // === MAPEO EXACTO DE TABLA F_AGE ===
        // Imagen 1
        public double? CODAGE { get; set; }
        public string TEMAGE { get; set; } = string.Empty;
        public string ZONAGE { get; set; } = string.Empty;
        public decimal? IMPAGE { get; set; }
        public double? COMAGE { get; set; }
        public string TCOAGE { get; set; } = string.Empty;
        public double? IVAAGE { get; set; }
        public double? IRPAGE { get; set; }
        public double? PIRAGE { get; set; }
        public DateTime? FALAGE { get; set; }
        public string FAXAGE { get; set; } = string.Empty;
        public string EMAAGE { get; set; } = string.Empty;
        public string WEBAGE { get; set; } = string.Empty;
        public string PAIAGE { get; set; } = string.Empty;
        public string PCOAGE { get; set; } = string.Empty;
        public string TEPAGE { get; set; } = string.Empty;
        public string CLAAGE { get; set; } = string.Empty;
        public string DNIAGE { get; set; } = string.Empty;
        public string RUTAGE { get; set; } = string.Empty;
        public string CUWAGE { get; set; } = string.Empty; // Código de usuario web
        public string CAWAGE { get; set; } = string.Empty; // Contraseña de usuario web
        public double? SUWAGE { get; set; } // Dar acceso a internet
        public string MEWAGE { get; set; } = string.Empty;
        public string CPOAGE { get; set; } = string.Empty;
        public string PROAGE { get; set; } = string.Empty;
        public string ENTAGE { get; set; } = string.Empty;

        // Imagen 2
        public string CFIAGE { get; set; } = string.Empty;
        public string DCOAGE { get; set; } = string.Empty;
        public string CUEAGE { get; set; } = string.Empty;
        public string BANAGE { get; set; } = string.Empty;
        public double? USAGE { get; set; }
        public double? CONAGE { get; set; }
        public string DOMAGE { get; set; } = string.Empty;
        public string NOMAGE { get; set; } = string.Empty;
        public string NOCAGE { get; set; } = string.Empty;
        public string MEMAGE { get; set; } = string.Empty;
        public string OBSAGE { get; set; } = string.Empty;
        public double? FORAGE { get; set; }
        public string LFOAGE { get; set; } = string.Empty;
        public DateTime? FFOAGE { get; set; }
        public string CFOAGE { get; set; } = string.Empty;
        public double? UREAGE { get; set; }
        public double? CURAGE { get; set; }
        public string URLAGE { get; set; } = string.Empty;
        public double? CATAGE { get; set; }
        public DateTime? FCCAGE { get; set; }
        public DateTime? FFCAGE { get; set; }
        public decimal? PUNAGE { get; set; }
        public decimal? CVEAGE { get; set; }
        public decimal? CREAGE { get; set; }

        // Imagen 3
        public decimal? PURAGE { get; set; }
        public double? JEQAGE { get; set; } // Jefe de Equipo (1=Si, 0=No)
        public decimal? CSAAGE { get; set; }
        public double? AGJAGE { get; set; }
        public double? DMWAGE { get; set; }
        public string FOTAGE { get; set; } = string.Empty;
        public string POBAGE { get; set; } = string.Empty;
        public string CTPAGE { get; set; } = string.Empty;

        public bool EsJefe => JEQAGE == 1;
    }

    public class GlobalConfig
    {
        public Dictionary<string, TaxItem> IvaConfig { get; set; } = new();
        public OrderSettings OrderSettings { get; set; } = new();
        public TunnelConfig Tunnel { get; set; } = new();
    }

    public class TunnelConfig
    {
        public bool Enabled { get; set; }
        public string Provider { get; set; } = "zrok"; 
        public string AuthToken { get; set; } = string.Empty;
        public string ReservedName { get; set; } = string.Empty;
        public int LocalPort { get; set; } = 44373;
        public string ManualUrl { get; set; } = string.Empty;
        public string CurrentUrl { get; set; } = string.Empty;
        public string Status { get; set; } = "Inactivo"; // Nuevo
        public string LastErrorMessage { get; set; } = string.Empty; // Nuevo
    }

    public class OrderSettings
    {
        public bool PermitirDescuentos { get; set; } = true;
        public string SeriePorDefecto { get; set; } = "1";
        public bool RestringirPedidosAAgentes { get; set; } = false;
        public List<double> AgentesHabilitados { get; set; } = new(); // Legacy
        public Dictionary<string, AgentPermission> PermisosAgentes { get; set; } = new(); // New per-agent config
    }

    public class AgentPermission
    {
        public bool AccesoMovil { get; set; } = true;
        public bool PermitirDescuentos { get; set; } = true;
        public bool PermitirEliminar { get; set; } = false;
        public bool SoloVerPedidosPropios { get; set; } = false; // Nueva restricción
        public string SeriePorDefecto { get; set; } = "1";
    }

    public class LoginRequest
    {
        public string Usuario { get; set; } = string.Empty;
        public string Clave { get; set; } = string.Empty;
    }

    public class UpdateDbRequest
    {
        public string NewPath { get; set; } = string.Empty;
        public string MasterPassword { get; set; } = string.Empty;
    }

    public class ValidateMasterRequest
    {
        public string Password { get; set; } = string.Empty;
    }

    public class TaxItem { public decimal IVA { get; set; } public decimal RE { get; set; } }

    public class BrowseResponse
    {
        public string CurrentPath { get; set; } = string.Empty;
        public string? ParentPath { get; set; }
        public List<FileEntry> Entries { get; set; } = new();
    }

    public class FileEntry
    {
        public string Name { get; set; } = string.Empty;
        public string FullPath { get; set; } = string.Empty;
        public bool IsDirectory { get; set; }
        public bool IsDatabase { get; set; }
    }

    public class Factura
    {
        public string TIPFAC { get; set; } = string.Empty;
        public double CODFAC { get; set; }
        public string REFFAC { get; set; } = string.Empty;
        public DateTime FECFAC { get; set; }
        public DateTime HORFAC { get; set; }
        public double ESTFAC { get; set; }
        public double CLIFAC { get; set; }
        public string CNOFAC { get; set; } = string.Empty;
        public string CDOFAC { get; set; } = string.Empty;
        public string CNIFAC { get; set; } = string.Empty;
        public string TELFAC { get; set; } = string.Empty;
        public decimal NET1FAC { get; set; }
        public decimal NET2FAC { get; set; }
        public decimal NET3FAC { get; set; }
        public decimal TOTFAC { get; set; }
        public double EMAFAC { get; set; } // Estado Correo (0 = Sin enviar, 1 = Enviado)
    }

    public class FacturaLinea
    {
        public string CodigoArticulo { get; set; } = string.Empty;
        public string DescripcionArticulo { get; set; } = string.Empty;
        public decimal Cantidad { get; set; }
        public decimal Precio { get; set; }
        public decimal Descuento1 { get; set; }
        public decimal Iva { get; set; }
        public decimal Total { get; set; }
    }

    public class Cobro
    {
        public double? CODCOB { get; set; }
        public DateTime? FECCOB { get; set; }
        public decimal? IMPCOB { get; set; }
        public string CPTCOB { get; set; } = string.Empty;
    }
}
