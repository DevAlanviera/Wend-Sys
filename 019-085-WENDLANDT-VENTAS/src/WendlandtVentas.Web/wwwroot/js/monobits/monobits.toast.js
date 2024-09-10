// Monobits Toast.  Versión: 1.2
// Snackbar con mensaje y animación
// última modificación: 21/oct/2015
// por Alejandro Quiñones Guzmán

// muestra el mensaje en un toast dependiendo del string guardado en la cookie.
// Guardar su valor por JS con Cookies.set('consola-toast', 'mensaje')
// o en C# con Response.Cookie["consola-toast"].Value = "mensaje"
$(document)
    .ready(function() {
        showMessageByResult();
    });

function showMessageByResult() {
    var msgCode = Cookies.get("consola-toast");

    if (typeof msgCode != "undefined") {
        if (msgCode != "") {
            showToast(msgCode);
        }
    }
}

function showToast(message) {
    monotoast(message);
    Cookies.remove("consola-toast");
}

function removeMessageCookie() {
    Cookies.remove("consola-toast");
}

// Función a llamar directamente
function monotoast(mensaje) {
    $("#alert").html(`<div class="mono-toast scene_element scene_element--fadeinup">${mensaje}</div>`);
    setTimeout(function() {
            $(".mono-toast")
                .animate({
                    bottom: -100,
                    opacity: 0
                });
        },
        3000);
}