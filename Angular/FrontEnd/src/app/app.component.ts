import { Component, OnInit } from '@angular/core';
import { SbdemoService } from './services/sbdemo.service';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css']
})
export class AppComponent {
  title = 'SBDemoFE';
  private all: any[] = [];
  SalesOrders: any[] = [];
  Invoices: any[] = []
  private interval: any;
  updating: boolean = false;

  constructor(private service: SbdemoService) {
  }

  ngOnInit(): void {
    this.loadOnlineTransactions();
    this.startTimer();
  }

  loadOnlineTransactions() {
    this.SalesOrders = [];
    this.Invoices = [];
    this.updating = true;
    this.service.getOnlineTransactions().subscribe((data: any) => {
      this.all = data;
      this.all.forEach(element => {
        if (element.type === "SO") {
          this.SalesOrders.push(element);
        } else {
          this.Invoices.push(element);
        }
      });
      this.updating = false;
    });
  }

  startTimer() {
    this.interval = setInterval(() => {
      if (!this.updating) {
        this.loadOnlineTransactions();
      }
    }, 2000)
  }
}
