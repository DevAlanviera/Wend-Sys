Feature: WMOD8_CambioRemisionFactura
	Se puede cambiar el tipo de pedido entre remisión y factura

@mytag
Scenario: Change sale type from "remision" to "facturacion"
	Given a remision sale type
	When its type is changed to facturacion
	Then the sale total is recalculated with taxes

Scenario: Change sale type from "facturacion" to "remision"
	Given a facturacion sale type
	When its type is changed to remision
	Then the sale total is recalculated with NO taxes