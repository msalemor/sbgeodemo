import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';

const ServiceURI: string = "https://localhost:44357/api/";
const config = new HttpHeaders().set('Content-Type', 'application/json');

function UUID(): string {
  return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function (c) {
    var r = Math.random() * 16 | 0, v = c == 'x' ? r : (r & 0x3 | 0x8);
    return v.toString(16);
  });
}

export interface SalesOrder {
  id: number,
  customerId: string
}

@Injectable({
  providedIn: 'root'
})
export class SbdemoService {

  constructor(private httpClient: HttpClient) { }

  getOnlineTransactions() {
    return this.httpClient.get(ServiceURI + "orders");
  }

  postSalesOrder(count: number = 1) {
    for (var i = 0; i < count; i++) {
      let obj: SalesOrder = { id: 0, customerId: UUID().substr(1, 7).toUpperCase() };
      this.httpClient.post(ServiceURI + "orders", JSON.stringify(obj), { headers: config }).subscribe((data) => { }, (error) => { console.error(error); });
    }
  }

}
