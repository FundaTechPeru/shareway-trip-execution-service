# language: es
Característica: Abordaje de pasajeros
  Para controlar quién sube al viaje
  Como conductor
  Quiero validar el código de abordaje de cada pasajero

  Antecedentes:
    Dado un viaje confirmado con un pasajero cuyo código es "1234"

  Escenario: Abordaje con código correcto
    Cuando el conductor valida el abordaje con el código "1234"
    Entonces el resultado del abordaje es "Abordado"
    Y el pasajero figura como abordado

  Escenario: Abordaje con código incorrecto
    Cuando el conductor valida el abordaje con el código "0000"
    Entonces el resultado del abordaje es "CodigoInvalido"
    Y el pasajero no figura como abordado

  Escenario: Bloqueo tras tres intentos fallidos
    Cuando el conductor valida el abordaje con el código "0000" 3 veces
    Y el conductor valida el abordaje con el código "1234"
    Entonces el resultado del abordaje es "Bloqueado"
    Y el pasajero queda bloqueado
