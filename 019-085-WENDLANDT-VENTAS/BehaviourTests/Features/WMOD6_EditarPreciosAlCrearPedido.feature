Feature: WMOD6_EditarPreciosAlCrearPedido
	Se edita el precio de los productos al crear un pedido

@mytag
Scenario: Edit items price when creating sale
	Given a sale concept which we need a special price
	When item price is updated
	Then sale Total equals the sum of the sale items price