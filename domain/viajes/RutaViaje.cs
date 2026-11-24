using System.Text.Json;
using System.Text.Json.Serialization;
using ShareWay.TripExecution.Domain.SeedWork;

namespace ShareWay.TripExecution.Domain.Viajes;

/// <summary>Punto de la ruta. <see cref="ReservaId"/> vincula la parada con su pasajero.</summary>
public sealed record PuntoRuta(
    string Etiqueta,
    double Latitud,
    double Longitud,
    string Tipo,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] Guid? ReservaId = null);

/// <summary>Ruta persistida en <c>Viaje.RutaJson</c>. El JSON conserva el formato del DTO existente.</summary>
public sealed record RutaViaje(
    DateTimeOffset SalidaProgramada,
    string Origen,
    string Destino,
    IReadOnlyList<PuntoRuta> Puntos,
    bool EsPrueba)
{
    private static readonly JsonSerializerOptions OpcionesJson = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static RutaViaje? DesdeJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            return JsonSerializer.Deserialize<RutaViaje>(json, OpcionesJson);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public string AJson() => JsonSerializer.Serialize(this, OpcionesJson);

    /// <summary>
    /// Ordena los puntos intermedios con nearest-neighbor desde el origen.
    /// El origen queda primero y el destino, al final.
    /// </summary>
    public RutaViaje Optimizada()
    {
        var puntos = Puntos ?? Array.Empty<PuntoRuta>();
        if (puntos.Count < 3) return this;

        var origenIndice = IndiceDe(puntos, "ORIGEN") ?? 0;
        var destinoIndice = IndiceDe(puntos, "DESTINO", desdeElFinal: true) ?? puntos.Count - 1;
        if (origenIndice == destinoIndice) return this;

        var origen = puntos[origenIndice];
        var destino = puntos[destinoIndice];
        var intermedios = puntos
            .Select((punto, indice) => (Punto: punto, Indice: indice))
            .Where(x => x.Indice != origenIndice && x.Indice != destinoIndice)
            .ToList();

        var ordenados = new List<PuntoRuta> { origen };
        var actual = origen;
        while (intermedios.Count > 0)
        {
            var siguiente = intermedios
                .OrderBy(x => DistanciaKm(actual, x.Punto))
                .ThenBy(x => x.Indice)
                .First();
            ordenados.Add(siguiente.Punto);
            intermedios.Remove(siguiente);
            actual = siguiente.Punto;
        }
        ordenados.Add(destino);

        return this with { Puntos = ordenados };
    }

    /// <summary>Quita la parada vinculada a una reserva (o, como respaldo, a una etiqueta).</summary>
    public RutaViaje SinParada(Guid reservaId, string? etiqueta = null)
    {
        var puntos = (Puntos ?? Array.Empty<PuntoRuta>()).ToList();
        var indice = puntos.FindIndex(p => p.Tipo == "PARADA" && p.ReservaId == reservaId);
        if (indice < 0 && !string.IsNullOrWhiteSpace(etiqueta))
            indice = puntos.FindIndex(p => p.Tipo == "PARADA" &&
                string.Equals(p.Etiqueta?.Trim(), etiqueta.Trim(), StringComparison.OrdinalIgnoreCase));

        if (indice < 0)
            throw new ReglaDeNegocioException(etiqueta is null
                ? $"La reserva {reservaId} no tiene una parada asociada en la ruta."
                : $"La parada '{etiqueta}' no existe en la ruta del viaje.");

        puntos.RemoveAt(indice);
        return this with { Puntos = puntos };
    }

    public bool TieneMismoOrden(RutaViaje otra) =>
        SalidaProgramada == otra.SalidaProgramada &&
        Origen == otra.Origen &&
        Destino == otra.Destino &&
        EsPrueba == otra.EsPrueba &&
        (Puntos ?? Array.Empty<PuntoRuta>()).SequenceEqual(otra.Puntos ?? Array.Empty<PuntoRuta>());

    private static int? IndiceDe(IReadOnlyList<PuntoRuta> puntos, string tipo, bool desdeElFinal = false)
    {
        if (desdeElFinal)
        {
            for (var i = puntos.Count - 1; i >= 0; i--)
                if (puntos[i].Tipo == tipo) return i;
        }
        else
        {
            for (var i = 0; i < puntos.Count; i++)
                if (puntos[i].Tipo == tipo) return i;
        }
        return null;
    }

    private static double DistanciaKm(PuntoRuta a, PuntoRuta b)
    {
        const double RadioKm = 6371.0088;
        static double Radianes(double grados) => grados * Math.PI / 180.0;

        var lat1 = Radianes(a.Latitud);
        var lat2 = Radianes(b.Latitud);
        var dLat = lat2 - lat1;
        var dLon = Radianes(b.Longitud - a.Longitud);
        var h = Math.Pow(Math.Sin(dLat / 2), 2) +
                Math.Cos(lat1) * Math.Cos(lat2) * Math.Pow(Math.Sin(dLon / 2), 2);
        return 2 * RadioKm * Math.Asin(Math.Min(1, Math.Sqrt(h)));
    }
}
