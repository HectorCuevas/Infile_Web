Imports System.Web.Services
Imports System.Web.Services.Protocols
Imports System.ComponentModel
Imports Infile.wsInfile
Imports Newtonsoft.Json
Imports System.Data.SqlClient
Imports System.IO
Imports System.Xml.Serialization

<System.Web.Services.WebService(Namespace:="http://tempuri.org/")> _
<System.Web.Services.WebServiceBinding(ConformsTo:=WsiProfiles.BasicProfile1_1)> _
<ToolboxItem(False)> _
Public Class Infile
    Inherits System.Web.Services.WebService

    <WebMethod()>
    Public Function FncProcesarDocumentoInfile(ByVal TipoFormulario As String, ByVal TipoDocumento As String, ByVal SerieDocumento As String, ByVal NoDocumento As String) As String
        Dim cObjDte As New dte
        Dim cObjws As New ingface
        Dim cObjRegistro As New requestDte
        Dim cObjResultado As New responseDte
        Dim lObjInFileLN As New InFileLN
        Dim lObjDteResultado As New ClsInfile
        Try
            System.Threading.Thread.CurrentThread.CurrentCulture = New Globalization.CultureInfo("es-GT")
            Dim lObjCulturizacion As New Globalization.DateTimeFormatInfo()
            lObjCulturizacion.DateSeparator = "/"
            System.Threading.Thread.CurrentThread.CurrentCulture.DateTimeFormat = lObjCulturizacion
            lObjInFileLN = FncProcesarDocumentoInfileDte(TipoFormulario, TipoDocumento, SerieDocumento, NoDocumento)
            cObjRegistro.dte = lObjInFileLN.Dte
            cObjRegistro.usuario = lObjInFileLN.Usuario
            cObjRegistro.clave = lObjInFileLN.Contrasena
            System.Net.ServicePointManager.Expect100Continue = True
            cObjResultado = cObjws.registrarDte(cObjRegistro)
            lObjDteResultado.DteValido = cObjResultado.valido
            lObjDteResultado.NumeroDte = IIf(cObjResultado.numeroDte Is Nothing, String.Empty, cObjResultado.numeroDte)
            lObjDteResultado.CAEDte = IIf(cObjResultado.cae Is Nothing, String.Empty, cObjResultado.cae)
            lObjDteResultado.DescripcionDte = IIf(cObjResultado.descripcion Is Nothing, String.Empty, cObjResultado.descripcion)
            GuardarFimayCaeDB(TipoFormulario, TipoDocumento, SerieDocumento, NoDocumento, lObjDteResultado)
        Catch ex As Exception
            lObjDteResultado.DteValido = False
            lObjDteResultado.NumeroDte = String.Empty
            lObjDteResultado.CAEDte = String.Empty
            lObjDteResultado.DescripcionDte = ex.Message
        End Try
        Return FncProcesaXmlEnviadoInfile(lObjInFileLN, lObjDteResultado)
    End Function

    'GENERA DTE
    Private Function FncProcesarDocumentoInfileDte(ByVal TipoFormulario As String, ByVal TipoDocumento As String, ByVal SerieDocumento As String, ByVal NoDocumento As String) As InFileLN
        'VARIABLES
        Dim cObjDte As New dte
        Dim lObjInFileLN As New InFileLN
        Dim lDstDatosGeneral As New DataSet
        Try
            'BUSCANDO INFORMACION A PROCESAR
            lDstDatosGeneral = FncBuscarDatos(TipoFormulario, TipoDocumento, SerieDocumento, NoDocumento)
            'DATOS DEL DOCUMENTO
            ProcesarDatosEstablecimiento(cObjDte, lDstDatosGeneral.Tables("DatosEstablecimiento")) 'DATOS DEL ESTABLECIMIENTO
            ProcesarDatosDocumentoComprador(cObjDte, lDstDatosGeneral.Tables("DatosComprador")) 'DATOS DEL COMPRADOR
            ProcesarValoresyMontos(cObjDte, lDstDatosGeneral.Tables("Documento")) 'DATOS DOCUMENTO
            ProcesarDatosVendedor(cObjDte, lDstDatosGeneral.Tables("DatosVendedor")) 'DATOS DEL VENDEDOR
            ProcesarDatosPersonalizados(cObjDte, lDstDatosGeneral.Tables("Documento")) 'DATOS PERSONALIZADOS
            ProcesarDetalleDocumento(cObjDte, lDstDatosGeneral.Tables("DocumentoDetalle")) 'DATOS DOCUMENTO DETALLE
            'RESULTADO
            lObjInFileLN.Usuario = lDstDatosGeneral.Tables("DatosConexion").Rows(0)("Usuario").ToString.Trim  'USUARIO WEB SERVICES
            lObjInFileLN.Contrasena = lDstDatosGeneral.Tables("DatosConexion").Rows(0)("Contrasena").ToString.Trim  'CONTRASEÑA WEB SERVICES
            lObjInFileLN.Dte = cObjDte
        Catch ex As Exception
            lObjInFileLN = New InFileLN
            Throw New Exception(ex.Message)
        End Try
        Return lObjInFileLN
    End Function

    'CONSULTA BASE DE DATOS DTE
    Private Function FncBuscarDatos(ByVal TipoFormulario As String, ByVal TipoDocumento As String, ByVal SerieDocumento As String, ByVal NoDocumento As String) As DataSet
        Dim lDstDatosGeneral As New DataSet
        Dim lStrConexionSql As String = String.Format(System.Configuration.ConfigurationManager.ConnectionStrings("InfileCnn").ConnectionString)
        Dim lStrSqlConsulta As String = String.Format("EXEC [xSpDocumentoInfile] '{0}','{1}','{2}','{3}'", TipoFormulario, TipoDocumento, SerieDocumento, NoDocumento)
        Using lSqlCnn As New SqlConnection(lStrConexionSql)
            lSqlCnn.Open()
            Using lSqlCmd As New SqlCommand(lStrSqlConsulta, lSqlCnn)
                Using SqlData As SqlDataAdapter = New SqlDataAdapter(lSqlCmd)
                    SqlData.Fill(lDstDatosGeneral)
                End Using
            End Using
        End Using
        lDstDatosGeneral.Tables(0).TableName = "DatosEstablecimiento"
        lDstDatosGeneral.Tables(1).TableName = "DatosComprador"
        lDstDatosGeneral.Tables(2).TableName = "DatosVendedor"
        lDstDatosGeneral.Tables(3).TableName = "Documento"
        lDstDatosGeneral.Tables(4).TableName = "DocumentoDetalle"
        lDstDatosGeneral.Tables(5).TableName = "DatosConexion"
        If lDstDatosGeneral.Tables("DatosEstablecimiento").Rows.Count = 0 Then Throw New Exception("Los datos de DatosEstablecimiento no se encuentra, favor de verificar información incompleta.")
        If lDstDatosGeneral.Tables("DatosComprador").Rows.Count = 0 Then Throw New Exception("Los datos de DatosComprador no se encuentra, favor de verificar información incompleta.")
        If lDstDatosGeneral.Tables("DatosVendedor").Rows.Count = 0 Then Throw New Exception("Los datos de DatosVendedor no se encuentra, favor de verificar información incompleta.")
        If lDstDatosGeneral.Tables("Documento").Rows.Count = 0 Then Throw New Exception("Los datos de Documento no se encuentra, favor de verificar información incompleta.")
        If lDstDatosGeneral.Tables("DocumentoDetalle").Rows.Count = 0 Then Throw New Exception("Los datos de DocumentoDetalle no se encuentra, favor de verificar información incompleta.")
        If lDstDatosGeneral.Tables("DatosConexion").Rows.Count = 0 Then Throw New Exception("Los datos de DatosConexion no se encuentra, favor de verificar información incompleta.")
        Return lDstDatosGeneral
    End Function

    'DATOS DEL ESTABLECIMIENTO
    Private Sub ProcesarDatosEstablecimiento(ByRef cObjDte As dte, ByVal lDtblDatos As DataTable)
        cObjDte.codigoEstablecimiento = lDtblDatos.Rows(0)("CodigoEstablecimiento").ToString.Trim
        cObjDte.idDispositivo = lDtblDatos.Rows(0)("CodigoDipositivo").ToString.Trim
        cObjDte.serieAutorizada = lDtblDatos.Rows(0)("SerieAutorizada").ToString.Trim
        cObjDte.numeroResolucion = lDtblDatos.Rows(0)("NoAutorizacion").ToString.Trim
        cObjDte.fechaResolucion = Convert.ToDateTime(lDtblDatos.Rows(0)("FechaResoluciones")).ToString()
        cObjDte.fechaResolucionSpecified = True
        cObjDte.tipoDocumento = lDtblDatos.Rows(0)("TipoDocumento").ToString.Trim
        cObjDte.serieDocumento = lDtblDatos.Rows(0)("SerieDocumento").ToString.Trim
    End Sub

    'DATOS DEL DOCUMENTO Y COMPRADOR
    Private Sub ProcesarDatosDocumentoComprador(ByRef cObjDte As dte, ByVal lDtblDatosComprador As DataTable)
        cObjDte.numeroDocumento = lDtblDatosComprador.Rows(0)("NoDocumento").ToString.Trim
        cObjDte.numeroDte = lDtblDatosComprador.Rows(0)("NoDocumento").ToString.Trim
        cObjDte.fechaDocumento = Convert.ToDateTime(lDtblDatosComprador.Rows(0)("FechaDocumento")).ToString()
        cObjDte.fechaDocumentoSpecified = True
        cObjDte.fechaAnulacion = Convert.ToDateTime(lDtblDatosComprador.Rows(0)("FechaDocumento")).ToString()
        cObjDte.fechaAnulacionSpecified = True
        cObjDte.estadoDocumento = lDtblDatosComprador.Rows(0)("EstadoDocumento").ToString.Trim
        cObjDte.codigoMoneda = lDtblDatosComprador.Rows(0)("TipoMoneda").ToString.Trim
        cObjDte.tipoCambio = lDtblDatosComprador.Rows(0)("TipoCambio").ToString.Trim
        cObjDte.tipoCambioSpecified = True
        cObjDte.nitComprador = lDtblDatosComprador.Rows(0)("NitComprador").ToString.Trim
        cObjDte.nombreComercialComprador = lDtblDatosComprador.Rows(0)("NombreComprador").ToString.Trim
        cObjDte.direccionComercialComprador = lDtblDatosComprador.Rows(0)("DireccionComprdor").ToString.Trim
        cObjDte.telefonoComprador = lDtblDatosComprador.Rows(0)("TelefonoComprador").ToString.Trim
        cObjDte.correoComprador = lDtblDatosComprador.Rows(0)("CorreoComprador").ToString.Trim
        cObjDte.regimen2989 = False
        cObjDte.municipioComprador = lDtblDatosComprador.Rows(0)("Municipio").ToString.Trim
        cObjDte.departamentoComprador = lDtblDatosComprador.Rows(0)("Departamento").ToString.Trim
    End Sub

    'VALORES Y MONTOS
    Private Sub ProcesarValoresyMontos(ByRef cObjDte As dte, ByVal lDtblDocumento As DataTable)
        cObjDte.importeBruto = lDtblDocumento.Rows(0)("ImporteBruto").ToString.Trim
        cObjDte.importeBrutoSpecified = True
        cObjDte.detalleImpuestosIva = lDtblDocumento.Rows(0)("TotalIva").ToString.Trim
        cObjDte.detalleImpuestosIvaSpecified = True
        cObjDte.importeNetoGravado = lDtblDocumento.Rows(0)("ImporteNetoGravado").ToString.Trim
        cObjDte.importeNetoGravadoSpecified = True
        cObjDte.importeDescuento = lDtblDocumento.Rows(0)("ImporteDescuento").ToString.Trim
        cObjDte.importeDescuentoSpecified = True
        cObjDte.importeTotalExento = lDtblDocumento.Rows(0)("ImporteTotalExento").ToString.Trim
        cObjDte.importeTotalExentoSpecified = True
        cObjDte.importeOtrosImpuestos = lDtblDocumento.Rows(0)("ImporteOtrosImpuesto").ToString.Trim
        cObjDte.importeOtrosImpuestosSpecified = True
        cObjDte.montoTotalOperacion = lDtblDocumento.Rows(0)("MontoTotal").ToString.Trim
        cObjDte.descripcionOtroImpuesto = "N/A"
    End Sub

    'DATOS DEL VENDEDOR
    Private Sub ProcesarDatosVendedor(ByRef cObjDte As dte, ByVal lDtblDatosVendedor As DataTable)
        cObjDte.nitVendedor = lDtblDatosVendedor.Rows(0)("NitVendedor").ToString.Trim
        cObjDte.nombreComercialRazonSocialVendedor = lDtblDatosVendedor.Rows(0)("RazonSocial").ToString.Trim
        cObjDte.nombreCompletoVendedor = lDtblDatosVendedor.Rows(0)("NombreComercial").ToString.Trim
        cObjDte.direccionComercialVendedor = lDtblDatosVendedor.Rows(0)("DireccionComercial").ToString.Trim
        cObjDte.municipioVendedor = lDtblDatosVendedor.Rows(0)("Municipio").ToString.Trim
        cObjDte.departamentoVendedor = lDtblDatosVendedor.Rows(0)("Departamento").ToString.Trim
        cObjDte.observaciones = lDtblDatosVendedor.Rows(0)("Observaciones").ToString.Trim
        cObjDte.regimenISR = lDtblDatosVendedor.Rows(0)("RegimenISR").ToString.Trim
        cObjDte.nitGFACE = lDtblDatosVendedor.Rows(0)("NitGface").ToString.Trim
    End Sub

    'CAMPOS PERSONALIZADOS
    Private Sub ProcesarDatosPersonalizados(ByRef cObjDte As dte, ByVal lDtblDocumento As DataTable)
        cObjDte.personalizado_01 = lDtblDocumento.Rows(0)("Personalizado1").ToString.Trim
        cObjDte.personalizado_02 = lDtblDocumento.Rows(0)("Personalizado2").ToString.Trim
        cObjDte.personalizado_03 = lDtblDocumento.Rows(0)("Personalizado3").ToString.Trim
        cObjDte.personalizado_04 = lDtblDocumento.Rows(0)("Personalizado4").ToString.Trim
        cObjDte.personalizado_05 = lDtblDocumento.Rows(0)("Personalizado5").ToString.Trim
        cObjDte.personalizado_06 = lDtblDocumento.Rows(0)("Personalizado6").ToString.Trim
        cObjDte.personalizado_07 = lDtblDocumento.Rows(0)("Personalizado7").ToString.Trim
        cObjDte.personalizado_08 = lDtblDocumento.Rows(0)("Personalizado8").ToString.Trim
        cObjDte.personalizado_09 = lDtblDocumento.Rows(0)("Personalizado9").ToString.Trim
        cObjDte.personalizado_10 = lDtblDocumento.Rows(0)("Personalizado10").ToString.Trim
        cObjDte.personalizado_11 = lDtblDocumento.Rows(0)("Personalizado11").ToString.Trim
        cObjDte.personalizado_12 = lDtblDocumento.Rows(0)("Personalizado12").ToString.Trim
        cObjDte.personalizado_13 = lDtblDocumento.Rows(0)("Personalizado13").ToString.Trim
        cObjDte.personalizado_14 = lDtblDocumento.Rows(0)("Personalizado14").ToString.Trim
        cObjDte.personalizado_15 = lDtblDocumento.Rows(0)("Personalizado15").ToString.Trim
        cObjDte.personalizado_16 = lDtblDocumento.Rows(0)("Personalizado16").ToString.Trim
        cObjDte.personalizado_17 = lDtblDocumento.Rows(0)("Personalizado17").ToString.Trim
        cObjDte.personalizado_18 = lDtblDocumento.Rows(0)("Personalizado18").ToString.Trim
        cObjDte.personalizado_19 = lDtblDocumento.Rows(0)("Personalizado19").ToString.Trim
        cObjDte.personalizado_20 = lDtblDocumento.Rows(0)("Personalizado20").ToString.Trim
    End Sub

    'DETALLE DEL DOCUMENTO
    Private Sub ProcesarDetalleDocumento(ByRef cObjDte As dte, ByVal lDtblDocumentoDetalle As DataTable)
        ReDim cObjDte.detalleDte(lDtblDocumentoDetalle.Rows.Count - 1)
        Dim cObjDetalleDte As New detalleDte
        For i As Integer = 0 To lDtblDocumentoDetalle.Rows.Count - 1
            cObjDetalleDte = New wsInfile.detalleDte
            cObjDetalleDte.cantidad = lDtblDocumentoDetalle.Rows(i)("Cantidad").ToString.Trim
            cObjDetalleDte.cantidadSpecified = True
            cObjDetalleDte.codigoProducto = lDtblDocumentoDetalle.Rows(i)("CodigoProducto").ToString.Trim
            cObjDetalleDte.descripcionProducto = lDtblDocumentoDetalle.Rows(i)("DescripcionProducto").ToString.Trim
            cObjDetalleDte.precioUnitario = lDtblDocumentoDetalle.Rows(i)("PrecioUnitario").ToString.Trim
            cObjDetalleDte.precioUnitarioSpecified = True
            cObjDetalleDte.montoBruto = lDtblDocumentoDetalle.Rows(i)("MontoBruto").ToString.Trim
            cObjDetalleDte.montoBrutoSpecified = True
            cObjDetalleDte.detalleImpuestosIva = lDtblDocumentoDetalle.Rows(i)("DetalleImpuesto").ToString.Trim
            cObjDetalleDte.detalleImpuestosIvaSpecified = True
            cObjDetalleDte.importeNetoGravado = lDtblDocumentoDetalle.Rows(i)("ImporteNeto").ToString.Trim
            cObjDetalleDte.importeNetoGravadoSpecified = True
            cObjDetalleDte.montoDescuento = lDtblDocumentoDetalle.Rows(i)("MontoDescuento").ToString.Trim
            cObjDetalleDte.montoDescuentoSpecified = True
            cObjDetalleDte.importeExento = lDtblDocumentoDetalle.Rows(i)("ImporteExento").ToString.Trim
            cObjDetalleDte.importeExentoSpecified = True
            cObjDetalleDte.importeOtrosImpuestos = lDtblDocumentoDetalle.Rows(i)("importeOtrosImpuestos").ToString.Trim
            cObjDetalleDte.importeOtrosImpuestosSpecified = True
            cObjDetalleDte.importeTotalOperacion = lDtblDocumentoDetalle.Rows(i)("ImporteTotalOperacion").ToString.Trim
            cObjDetalleDte.importeTotalOperacionSpecified = True
            cObjDetalleDte.unidadMedida = lDtblDocumentoDetalle.Rows(i)("UnidadMedida").ToString.Trim
            cObjDetalleDte.tipoProducto = lDtblDocumentoDetalle.Rows(i)("TipoProducto").ToString.Trim
            'CAMPOS PERSONALIZADOS
            cObjDetalleDte.personalizado_01 = lDtblDocumentoDetalle.Rows(i)("Personalizado1").ToString.Trim
            cObjDetalleDte.personalizado_02 = lDtblDocumentoDetalle.Rows(i)("Personalizado2").ToString.Trim
            cObjDetalleDte.personalizado_03 = lDtblDocumentoDetalle.Rows(i)("Personalizado3").ToString.Trim
            cObjDetalleDte.personalizado_04 = lDtblDocumentoDetalle.Rows(i)("Personalizado4").ToString.Trim
            cObjDetalleDte.personalizado_05 = lDtblDocumentoDetalle.Rows(i)("Personalizado5").ToString.Trim
            cObjDetalleDte.personalizado_06 = lDtblDocumentoDetalle.Rows(i)("Personalizado6").ToString.Trim
            'AGREGANDO LISTA
            cObjDte.detalleDte(i) = cObjDetalleDte
        Next
    End Sub

    'GUARDAR XML ENVIADO
    Private Function FncProcesaXmlEnviadoInfile(ByVal lObjInFileLN As InFileLN, ByVal lObjDteResultado As ClsInfile) As String
        Dim lTxtWrite As New StringWriter()
        Dim lObjXml As New XmlSerializer(lObjInFileLN.GetType)
        lObjXml.Serialize(lTxtWrite, lObjInFileLN)
        If Not IO.Directory.Exists("C:\Infile") Then
            IO.Directory.CreateDirectory("C:\Infile")
        End If
        If Not IO.Directory.Exists(String.Format("C:\Infile\{0}", lObjInFileLN.Dte.tipoDocumento)) Then
            IO.Directory.CreateDirectory(String.Format("C:\Infile\{0}", lObjInFileLN.Dte.tipoDocumento))
        End If
        If Not IO.Directory.Exists(String.Format("C:\Infile\{0}\{1}", lObjInFileLN.Dte.tipoDocumento, lObjInFileLN.Dte.serieDocumento)) Then
            IO.Directory.CreateDirectory(String.Format("C:\Infile\{0}\{1}", lObjInFileLN.Dte.tipoDocumento, lObjInFileLN.Dte.serieDocumento))
        End If
        'GUARDANDO RESULTADO INFILE WS - POST
        If Not File.Exists(String.Format("C:\Infile\{0}\{1}\{2}-{3}.xml",
                                         lObjInFileLN.Dte.tipoDocumento,
                                         lObjInFileLN.Dte.serieDocumento,
                                         lObjInFileLN.Dte.serieAutorizada,
                                         lObjInFileLN.Dte.numeroDte)) Then
            File.WriteAllText(String.Format("C:\Infile\{0}\{1}\{2}-{3}.xml",
                                            lObjInFileLN.Dte.tipoDocumento,
                                             lObjInFileLN.Dte.serieDocumento,
                                            lObjInFileLN.Dte.serieAutorizada,
                                            lObjInFileLN.Dte.numeroDte),
                                            lTxtWrite.ToString())
        End If
        lTxtWrite = New StringWriter()
        lObjXml = New XmlSerializer(lObjDteResultado.GetType)
        lObjXml.Serialize(lTxtWrite, lObjDteResultado)
        'GUARDANDO RESULTADO INFILE WS - RESPONSE
        If Not File.Exists(String.Format("C:\Infile\{0}\{1}\{2}-{3}_Resultado.xml",
                                         lObjInFileLN.Dte.tipoDocumento,
                                         lObjInFileLN.Dte.serieDocumento,
                                         lObjInFileLN.Dte.serieAutorizada,
                                         lObjInFileLN.Dte.numeroDte)) Then
            File.WriteAllText(String.Format("C:\Infile\{0}\{1}\{2}-{3}_Resultado.xml",
                                            lObjInFileLN.Dte.tipoDocumento,
                                             lObjInFileLN.Dte.serieDocumento,
                                            lObjInFileLN.Dte.serieAutorizada,
                                            lObjInFileLN.Dte.numeroDte),
                                            lTxtWrite.ToString())
        End If
        Return lTxtWrite.ToString()
    End Function

    'GUARDAR FIRMA Y CAE DB
    Private Sub GuardarFimayCaeDB(ByVal TipoFormulario As String, ByVal TipoDocumento As String, ByVal SerieDocumento As String, ByVal NoDocumento As String, ByVal lObjDteResultado As ClsInfile)
        If lObjDteResultado.DteValido Then
            Dim lDstDatosGeneral As New DataSet
            Dim lStrConexionSql As String = String.Format(System.Configuration.ConfigurationManager.ConnectionStrings("InfileCnn").ConnectionString)
            Dim lStrSqlConsulta As String = String.Format("EXEC [xSpDocumentoInfileCae] '{0}','{1}','{2}','{3}','{4}','{5}'",
                                                          TipoFormulario, TipoDocumento, SerieDocumento, NoDocumento, lObjDteResultado.NumeroDte, lObjDteResultado.CAEDte)
            Using lSqlCnn As New SqlConnection(lStrConexionSql)
                lSqlCnn.Open()
                Using lSqlCmd As New SqlCommand(lStrSqlConsulta, lSqlCnn)
                    lSqlCmd.ExecuteNonQuery()
                End Using
                lSqlCnn.Close()
            End Using
        End If
    End Sub
End Class