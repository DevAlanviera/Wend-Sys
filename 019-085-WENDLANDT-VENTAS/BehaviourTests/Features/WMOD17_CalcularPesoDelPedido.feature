Feature: WMOD17_CalcularPesoDelPedido
	Se calcula el peso del pedido en base a la sumatoria de los elementos

@mytag
Scenario: Calculate sale weight
	Given a sale concept which we want to know the sale weight
	When sale item list is updated
	Then sale weight equals the sum of the current items weight