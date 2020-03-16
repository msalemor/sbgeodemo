import { Component, OnInit } from '@angular/core';
import { SbdemoService } from '../services/sbdemo.service';
import {NgForm} from '@angular/forms';

@Component({
  selector: 'app-sidebar',
  templateUrl: './sidebar.component.html',
  styleUrls: ['./sidebar.component.css']
})
export class SidebarComponent implements OnInit {

  orders = '1';

  constructor(private service : SbdemoService) { }

  ngOnInit(): void {
  }

  onSubmit(f: NgForm) {
    //console.log(f.value);  // { first: '', last: '' }
    this.service.postSalesOrder(Number(this.orders));
  }

}
