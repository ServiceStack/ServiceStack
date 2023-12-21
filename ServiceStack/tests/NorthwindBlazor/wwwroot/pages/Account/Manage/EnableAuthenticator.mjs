import { addScript, $1 } from "@servicestack/client"
const loadJs = addScript('lib/js/qrcode.min.js')

export default {
    async load() {
        await loadJs
        new QRCode($1("#qrCode"), $1('#qrCodeData').dataset.url)
    }
}
